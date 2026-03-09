using System;
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
    [Route("api/tasks/{taskId}/evaluation")]
    public class TaskEvaluationController : ControllerBase
    {
        private readonly ITaskEvaluationRepository _repo;
        private readonly ITaskRepository _taskRepo;
        private readonly ICurrentUserAccessor _cu;
        private readonly ITeamDirectory _teams;
public TaskEvaluationController(
            ITaskEvaluationRepository repo,
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
        public async Task<IActionResult> Create(decimal taskId, [FromBody] CreateTaskEvaluationRequest req)
        {
            if (req == null) return BadRequest();

            var me = _cu.Get();

            // ✅ 평가자는 MANAGER/ADMIN만
            if (me.Role != UserRole.MANAGER && me.Role != UserRole.ADMIN)
                return Forbid();

            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task == null) return NotFound();

            // ✅ MANAGER는 팀 범위만
            if (!CanManagerEvaluate(me, task)) return Forbid();

            // evaluatorId는 토큰 기반으로 강제
            ObjectProp.SetUserId(req, "EvaluatorId", me.UserId);

            if (req.ScoreQuality < 1 || req.ScoreQuality > 5) return BadRequest("scoreQuality must be 1~5");
            if (req.ScoreTimeliness < 1 || req.ScoreTimeliness > 5) return BadRequest("scoreTimeliness must be 1~5");
            if (req.ScoreCommunication < 1 || req.ScoreCommunication > 5) return BadRequest("scoreCommunication must be 1~5");

            var evalId = await _repo.CreateAsync(taskId, req);
            var rows = await _repo.GetByTaskIdAsync(taskId);

            return Ok(new { evalId, items = rows });
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

        private bool CanManagerEvaluate(CurrentUser me, object taskRow)
        {
            if (me.Role == UserRole.ADMIN) return true;

            var reporterId = ObjectProp.GetLong(taskRow, "ReporterId", "REPORTER_ID");
            var assigneeId = ObjectProp.GetLong(taskRow, "AssigneeId", "ASSIGNEE_ID");

            if (reporterId.HasValue && IsUserInMyTeam(reporterId.Value, me.TeamId)) return true;
            if (assigneeId.HasValue && IsUserInMyTeam(assigneeId.Value, me.TeamId)) return true;

            return false;
        }

        private bool IsUserInMyTeam(long userId, string myTeamId)
        {
            return _teams.IsUserInTeam(userId, myTeamId);
        }
    }
}