using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Models;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _repo;
        private readonly ICurrentUserAccessor _cu;
        private readonly ITeamDirectory _teams;

        public TasksController(ITaskRepository repo, ICurrentUserAccessor cu, ITeamDirectory teams)
        {
            _repo = repo;
            _cu = cu;
            _teams = teams;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string status = null,
            [FromQuery] decimal? assigneeId = null,
            [FromQuery] WorklogScope scope = WorklogScope.mine)
        {
            var me = _cu.Get();

            var deny = TryEnsureScopeAllowed(me, scope);
            if (deny != null) return deny;

            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedStatus) && !TaskStatusGuard.IsAllowed(normalizedStatus))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            IEnumerable<long> teamUserIds = null;
            if (scope == WorklogScope.team)
            {
                if (string.IsNullOrWhiteSpace(me.TeamId))
                    return StatusCode(403, new { message = "Team scope requires a team assignment." });

                teamUserIds = _teams.GetTeamUserIds(me.TeamId);
            }

            var rows = await _repo.GetListScopedAsync(
                normalizedStatus,
                assigneeId,
                scope,
                me.UserId,
                teamUserIds
            );

            return Ok(rows);
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> Get(decimal taskId)
        {
            var me = _cu.Get();

            var row = await _repo.GetByIdAsync(taskId);
            if (row == null) return NotFound();

            var deny = TryEnsureCanViewTask(me, row);
            if (deny != null) return deny;

            return Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest req)
        {
            if (req == null) return BadRequest();

            var me = _cu.Get();

            ObjectProp.SetUserId(req, "ReporterId", me.UserId);

            if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("title required");
            if (req.Priority < 1 || req.Priority > 5) return BadRequest("priority must be 1~5");
            if (req.ProgressPct < 0 || req.ProgressPct > 100) return BadRequest("progressPct must be 0~100");

            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "TODO" : req.Status.Trim().ToUpperInvariant();
            req.TaskType = TaskIssueTypeGuard.NormalizeOrDefault(req.TaskType);

            if (!TaskStatusGuard.IsAllowed(req.Status))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            if (!TaskIssueTypeGuard.IsAllowed(req.TaskType))
                return BadRequest("Invalid taskType. Allowed: " + TaskIssueTypeGuard.AllowedList());

            if (me.Role == UserRole.USER && string.Equals(req.Status, "CANCELED", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only MANAGER/ADMIN can create issue with CANCELED status." });

            if (string.Equals(req.Status, "DONE", StringComparison.OrdinalIgnoreCase))
                req.ProgressPct = 100;

            var id = await _repo.CreateAsync(req);
            var created = await _repo.GetByIdAsync(id);

            return Created($"/api/tasks/{id}", created);
        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> Update(decimal taskId, [FromBody] UpdateTaskRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("title required");
            if (req.Priority < 1 || req.Priority > 5) return BadRequest("priority must be 1~5");
            if (req.ProgressPct < 0 || req.ProgressPct > 100) return BadRequest("progressPct must be 0~100");

            var me = _cu.Get();

            var existing = await _repo.GetByIdAsync(taskId);
            if (existing == null) return NotFound();

            var deny = TryEnsureCanEditTask(me, existing);
            if (deny != null) return deny;

            req.Status = string.IsNullOrWhiteSpace(req.Status) ? existing.Status : req.Status.Trim().ToUpperInvariant();
            req.TaskType = string.IsNullOrWhiteSpace(req.TaskType)
                ? TaskIssueTypeGuard.NormalizeOrDefault(existing.TaskType)
                : TaskIssueTypeGuard.NormalizeOrDefault(req.TaskType);

            if (!TaskStatusGuard.IsAllowed(req.Status))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            if (!TaskIssueTypeGuard.IsAllowed(req.TaskType))
                return BadRequest("Invalid taskType. Allowed: " + TaskIssueTypeGuard.AllowedList());

            var existingType = TaskIssueTypeGuard.NormalizeOrDefault(existing.TaskType);
            if (me.Role == UserRole.USER && !string.Equals(existingType, req.TaskType, StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only MANAGER/ADMIN can change taskType." });

            if (!TaskWorkflowGuard.TryValidateTransition(existing.Status, req.Status, req.TaskType, me.Role, out var workflowError))
                return BadRequest(workflowError);

            if (string.Equals(req.Status, "DONE", StringComparison.OrdinalIgnoreCase))
                req.ProgressPct = 100;

            var ok = await _repo.UpdateAsync(taskId, req);
            if (!ok) return NotFound();

            var row = await _repo.GetByIdAsync(taskId);
            return Ok(row);
        }

        [HttpPatch("{taskId}/status")]
        public async Task<IActionResult> UpdateStatus(decimal taskId, [FromQuery] string status, [FromQuery] int? progressPct = null)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("status required");

            status = status.Trim().ToUpperInvariant();
            if (!TaskStatusGuard.IsAllowed(status))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            if (progressPct.HasValue && (progressPct.Value < 0 || progressPct.Value > 100))
                return BadRequest("progressPct must be 0~100");

            var me = _cu.Get();

            var existing = await _repo.GetByIdAsync(taskId);
            if (existing == null) return NotFound();

            var deny = TryEnsureCanEditTask(me, existing);
            if (deny != null) return deny;

            if (!TaskWorkflowGuard.TryValidateTransition(existing.Status, status, existing.TaskType, me.Role, out var workflowError))
                return BadRequest(workflowError);

            var ok = await _repo.UpdateStatusAsync(taskId, status, progressPct);
            if (!ok) return NotFound();

            var row = await _repo.GetByIdAsync(taskId);
            return Ok(row);
        }

        private IActionResult TryEnsureScopeAllowed(CurrentUser me, WorklogScope scope)
        {
            if (!ScopeGuard.IsAllowed(me, scope))
                return StatusCode(403, new { message = ScopeGuard.GetDenyMessage(scope) });

            return null;
        }

        private IActionResult TryEnsureCanViewTask(CurrentUser me, TaskItem taskRow)
        {
            if (CanViewTask(me, taskRow)) return null;
            return StatusCode(403, new { message = "You are not allowed to view this task." });
        }

        private IActionResult TryEnsureCanEditTask(CurrentUser me, TaskItem taskRow)
        {
            if (CanEditTask(me, taskRow)) return null;
            return StatusCode(403, new { message = "You are not allowed to modify this task." });
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

        private bool CanEditTask(CurrentUser me, TaskItem taskRow)
        {
            return CanViewTask(me, taskRow);
        }

        private bool IsUserInMyTeam(long userId, string myTeamId)
        {
            return _teams.IsUserInTeam(userId, myTeamId);
        }
    }
}
