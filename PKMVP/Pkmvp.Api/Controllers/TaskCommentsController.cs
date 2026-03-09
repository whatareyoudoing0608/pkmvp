using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Models;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tasks/{taskId}/comments")]
    public class TaskCommentsController : ControllerBase
    {
        private static readonly Regex MentionRegex = new Regex(@"(?<!\w)@(?<token>[A-Za-z0-9_.-]{2,30})", RegexOptions.Compiled);

        private readonly ITaskCommentRepository _comments;
        private readonly ITaskRepository _tasks;
        private readonly INotificationRepository _notifications;
        private readonly ICurrentUserAccessor _cu;
        private readonly ITeamDirectory _teams;
        private readonly IConfiguration _cfg;

        public TaskCommentsController(
            ITaskCommentRepository comments,
            ITaskRepository tasks,
            INotificationRepository notifications,
            ICurrentUserAccessor cu,
            ITeamDirectory teams,
            IConfiguration cfg)
        {
            _comments = comments;
            _tasks = tasks;
            _notifications = notifications;
            _cu = cu;
            _teams = teams;
            _cfg = cfg;
        }

        [HttpGet]
        public async Task<IActionResult> List(decimal taskId)
        {
            var me = _cu.Get();
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            var deny = TryEnsureCanViewTask(me, task);
            if (deny != null) return deny;

            var rows = await _comments.ListByTaskIdAsync(taskId);
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Create(decimal taskId, [FromBody] CreateTaskCommentRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest("content required");

            var content = req.Content.Trim();
            if (content.Length > 4000) return BadRequest("content max length is 4000");

            var me = _cu.Get();
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            var deny = TryEnsureCanViewTask(me, task);
            if (deny != null) return deny;

            if (req.ParentCommentId.HasValue)
            {
                var parent = await _comments.GetByIdAsync(taskId, req.ParentCommentId.Value);
                if (parent == null) return BadRequest("parent comment not found");
            }

            var id = await _comments.CreateAsync(taskId, req.ParentCommentId, me.UserId, content);
            var created = await _comments.GetByIdAsync(taskId, id);

            await CreateCommentNotificationsAsync(me.UserId, task, content);

            return Created($"/api/tasks/{taskId}/comments/{id}", created);
        }

        [HttpPatch("{commentId}")]
        public async Task<IActionResult> Update(decimal taskId, decimal commentId, [FromBody] UpdateTaskCommentRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest("content required");

            var content = req.Content.Trim();
            if (content.Length > 4000) return BadRequest("content max length is 4000");

            var me = _cu.Get();
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            var deny = TryEnsureCanViewTask(me, task);
            if (deny != null) return deny;

            var existing = await _comments.GetByIdAsync(taskId, commentId);
            if (existing == null) return NotFound();

            var ok = await _comments.UpdateAsync(taskId, commentId, me.UserId, content, IsPrivileged(me));
            if (!ok) return StatusCode(403, new { message = "You are not allowed to edit this comment." });

            var updated = await _comments.GetByIdAsync(taskId, commentId);
            return Ok(updated);
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> Delete(decimal taskId, decimal commentId)
        {
            var me = _cu.Get();
            var task = await _tasks.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            var deny = TryEnsureCanViewTask(me, task);
            if (deny != null) return deny;

            var existing = await _comments.GetByIdAsync(taskId, commentId);
            if (existing == null) return NotFound();

            var ok = await _comments.SoftDeleteAsync(taskId, commentId, me.UserId, IsPrivileged(me));
            if (!ok) return StatusCode(403, new { message = "You are not allowed to delete this comment." });

            return NoContent();
        }

        private static bool IsPrivileged(CurrentUser me)
        {
            return me.Role == UserRole.ADMIN || me.Role == UserRole.MANAGER;
        }

        private async Task CreateCommentNotificationsAsync(long actorId, TaskItem task, string content)
        {
            var commentTargets = new HashSet<long>();

            var reporterId = (long)task.ReporterId;
            if (reporterId != actorId)
                commentTargets.Add(reporterId);

            if (task.AssigneeId.HasValue)
            {
                var assigneeId = (long)task.AssigneeId.Value;
                if (assigneeId != actorId)
                    commentTargets.Add(assigneeId);
            }

            var mentionTargets = new HashSet<long>(ExtractMentionUserIds(content).Where(x => x != actorId));

            var preview = content.Length > 120 ? content.Substring(0, 120) + "..." : content;

            if (mentionTargets.Count > 0)
            {
                await _notifications.CreateForUsersAsync(
                    mentionTargets,
                    "TASK_MENTION",
                    $"Task #{task.TaskId}: you were mentioned",
                    preview,
                    "TASK",
                    task.TaskId);

                commentTargets.ExceptWith(mentionTargets);
            }

            if (commentTargets.Count > 0)
            {
                await _notifications.CreateForUsersAsync(
                    commentTargets,
                    "TASK_COMMENT",
                    $"Task #{task.TaskId}: new comment",
                    preview,
                    "TASK",
                    task.TaskId);
            }
        }

        private IReadOnlyList<long> ExtractMentionUserIds(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Array.Empty<long>();

            var loginMap = GetLoginIdToUserIdMap();
            var ids = new HashSet<long>();

            foreach (Match m in MentionRegex.Matches(content))
            {
                var token = m.Groups["token"].Value;
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var normalized = token.Trim();

                if (long.TryParse(normalized, out var rawId) && rawId > 0)
                {
                    ids.Add(rawId);
                    continue;
                }

                if (normalized.StartsWith("user", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(normalized.Substring(4), out var userStyleId)
                    && userStyleId > 0)
                {
                    ids.Add(userStyleId);
                    continue;
                }

                if (loginMap.TryGetValue(normalized, out var mappedId) && mappedId > 0)
                {
                    ids.Add(mappedId);
                }
            }

            return ids.ToList();
        }

        private Dictionary<string, long> GetLoginIdToUserIdMap()
        {
            var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            var users = _cfg.GetSection("Auth:Users").GetChildren();
            foreach (var u in users)
            {
                var loginId = u["LoginId"];
                var uidStr = u["UserId"];
                if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(uidStr))
                    continue;

                if (!long.TryParse(uidStr, out var uid) || uid <= 0)
                    continue;

                map[loginId.Trim()] = uid;
            }

            return map;
        }

        private IActionResult TryEnsureCanViewTask(CurrentUser me, TaskItem taskRow)
        {
            if (CanViewTask(me, taskRow)) return null;
            return StatusCode(403, new { message = "You are not allowed to view this task." });
        }

        private bool CanViewTask(CurrentUser me, TaskItem taskRow)
        {
            if (me.Role == UserRole.ADMIN) return true;

            var reporterId = (long)taskRow.ReporterId;
            var assigneeId = taskRow.AssigneeId.HasValue ? (long?)taskRow.AssigneeId.Value : null;

            if (reporterId == me.UserId) return true;
            if (assigneeId.HasValue && assigneeId.Value == me.UserId) return true;

            if (me.Role == UserRole.MANAGER)
            {
                if (IsUserInMyTeam(reporterId, me.TeamId)) return true;
                if (assigneeId.HasValue && IsUserInMyTeam(assigneeId.Value, me.TeamId)) return true;
            }

            return false;
        }

        private bool IsUserInMyTeam(long userId, string myTeamId)
        {
            return _teams.IsUserInTeam(userId, myTeamId);
        }
    }
}
