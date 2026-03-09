using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Models;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tasks/{taskId}/progress")]
    public class TaskProgressController : ControllerBase
    {
        private readonly ITaskProgressRepository _repo;
        private readonly ITaskRepository _taskRepo;
        private readonly ICurrentUserAccessor _cu;
        private readonly ITeamDirectory _teams;

        public TaskProgressController(
            ITaskProgressRepository repo,
            ITaskRepository taskRepo,
            ICurrentUserAccessor cu,
            ITeamDirectory teams)
        {
            _repo = repo;
            _taskRepo = taskRepo;
            _cu = cu;
            _teams = teams;
        }

        [HttpGet]
        public async Task<IActionResult> List(decimal taskId)
        {
            var me = _cu.Get();

            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            if (!CanViewTask(me, task)) return Forbid();

            var rows = await _repo.GetByTaskIdAsync(taskId);
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Create(decimal taskId, [FromBody] CreateTaskProgressRequest req)
        {
            if (req == null) return BadRequest();

            var me = _cu.Get();

            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            if (!CanViewTask(me, task)) return Forbid();

            ObjectProp.SetUserId(req, "AuthorId", me.UserId);

            if (req.ProgressPct < 0 || req.ProgressPct > 100) return BadRequest("progressPct must be 0~100");
            if (req.SpentMinutes < 0) return BadRequest("spentMinutes must be >= 0");

            req.Status = string.IsNullOrWhiteSpace(req.Status) ? task.Status : req.Status.Trim().ToUpperInvariant();
            if (!TaskStatusGuard.IsAllowed(req.Status))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            if (!TaskWorkflowGuard.TryValidateTransition(task.Status, req.Status, task.TaskType, me.Role, out var workflowError))
                return BadRequest(workflowError);

            try
            {
                var id = await _repo.CreateAsync(taskId, req);
                await _taskRepo.UpdateStatusAsync(taskId, req.Status, req.ProgressPct);

                var rows = await _repo.GetByTaskIdAsync(taskId);
                return Ok(new { progressId = id, items = rows });
            }
            catch (OracleException ex) when (ex.Number == 2290)
            {
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());
            }
        }

        private bool CanViewTask(CurrentUser me, object taskRow)
        {
            if (me.Role == UserRole.ADMIN) return true;

            var reporterId = ObjectProp.GetLong(taskRow, "ReporterId", "REPORTER_ID");
            var assigneeId = ObjectProp.GetLong(taskRow, "AssigneeId", "ASSIGNEE_ID");

            if (reporterId.HasValue && reporterId.Value == me.UserId) return true;
            if (assigneeId.HasValue && assigneeId.Value == me.UserId) return true;

            if (me.Role == UserRole.MANAGER)
            {
                if (reporterId.HasValue && IsUserInMyTeam(reporterId.Value, me.TeamId)) return true;
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
