using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Filters;
using Pkmvp.Api.Models;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/worklogs")]
    public class DailyWorklogsController : ControllerBase
    {
        private readonly ICurrentUserAccessor _cu;
        private readonly IDailyWorklogRepository _repo;
        private readonly ITeamDirectory _teams;

        public DailyWorklogsController(ICurrentUserAccessor cu, IDailyWorklogRepository repo, ITeamDirectory teams)
        {
            _cu = cu;
            _repo = repo;
            _teams = teams;
        }

        private bool IsSameTeamByDirectory(long reporterId, string myTeamId)
        {
            return !string.IsNullOrWhiteSpace(myTeamId) && _teams.IsUserInTeam(reporterId, myTeamId);
        }

        [HttpPost]
        [RejectJsonProps("reporterId", "ReporterId", "authorId", "AuthorId", "teamId", "TeamId", "userId", "UserId")]
        public IActionResult Create([FromBody] CreateDailyWorklogRequest req)
        {
            var me = _cu.Get();
            var id = _repo.CreateHeader(req.WorkDate, me.UserId, me.TeamId, me.UserId, req.Summary);
            return Ok(new { worklogId = id });
        }

        [HttpGet]
        public IActionResult List(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] WorklogScope scope = WorklogScope.mine)
        {
            var me = _cu.Get();

            if (!ScopeGuard.IsAllowed(me, scope))
                return StatusCode(403, new { message = ScopeGuard.GetDenyMessage(scope) });

            if (scope == WorklogScope.team && string.IsNullOrWhiteSpace(me.TeamId))
                return StatusCode(403, new { message = "Team scope requires a team assignment." });

            var rows = _repo.List(fromDate, toDate, scope.ToString(), me.UserId, me.TeamId);
            return Ok(rows);
        }

        [HttpPost("{worklogId}/submit")]
        public IActionResult Submit(long worklogId)
        {
            var me = _cu.Get();
            var wl = _repo.GetHeaderAuth(worklogId);
            if (wl == null) return NotFound();

            if (!string.Equals(wl.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
                return Conflict("Worklog is not DRAFT.");

            var sameTeamByDirectory = IsSameTeamByDirectory(wl.ReporterId, me.TeamId);
            if (!WorklogGuard.CanEditDraft(me, wl.ReporterId, wl.ReporterTeamId, sameTeamByDirectory))
                return StatusCode(403, new { message = "You are not allowed to submit this worklog." });

            _repo.Submit(worklogId, me.UserId);
            return Ok();
        }

        [HttpPost("{worklogId}/approve")]
        [RejectJsonProps("evaluatorId", "EvaluatorId", "authorId", "AuthorId", "actorId", "ActorId", "reporterId", "ReporterId")]
        public IActionResult Approve(long worklogId, [FromBody] ApproveRejectRequest req)
        {
            var me = _cu.Get();
            var wl = _repo.GetHeaderAuth(worklogId);
            if (wl == null) return NotFound();

            if (!string.Equals(wl.Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return Conflict("Worklog is not SUBMITTED.");

            var sameTeamByDirectory = IsSameTeamByDirectory(wl.ReporterId, me.TeamId);
            if (!WorklogGuard.CanApproveReject(me, wl.ReporterId, wl.ReporterTeamId, sameTeamByDirectory))
                return StatusCode(403, new { message = "You are not allowed to approve this worklog." });

            _repo.Approve(worklogId, me.UserId, req.Score, req.CommentTxt);
            return Ok();
        }

        [HttpPost("{worklogId}/reject")]
        [RejectJsonProps("evaluatorId", "EvaluatorId", "authorId", "AuthorId", "actorId", "ActorId", "reporterId", "ReporterId")]
        public IActionResult Reject(long worklogId, [FromBody] ApproveRejectRequest req)
        {
            var me = _cu.Get();
            var wl = _repo.GetHeaderAuth(worklogId);
            if (wl == null) return NotFound();

            if (!string.Equals(wl.Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return Conflict("Worklog is not SUBMITTED.");

            var sameTeamByDirectory = IsSameTeamByDirectory(wl.ReporterId, me.TeamId);
            if (!WorklogGuard.CanApproveReject(me, wl.ReporterId, wl.ReporterTeamId, sameTeamByDirectory))
                return StatusCode(403, new { message = "You are not allowed to reject this worklog." });

            _repo.Reject(worklogId, me.UserId, req.Score, req.CommentTxt);
            return Ok();
        }

        [HttpPost("{worklogId}/items")]
        [RejectJsonProps("authorId", "AuthorId", "actorId", "ActorId", "reporterId", "ReporterId")]
        public IActionResult AddItem(long worklogId, [FromBody] CreateDailyWorklogItemRequest req)
        {
            var me = _cu.Get();
            var wl = _repo.GetHeaderAuth(worklogId);
            if (wl == null) return NotFound();

            if (!string.Equals(wl.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
                return Conflict("Worklog is not DRAFT.");

            var sameTeamByDirectory = IsSameTeamByDirectory(wl.ReporterId, me.TeamId);
            if (!WorklogGuard.CanEditDraft(me, wl.ReporterId, wl.ReporterTeamId, sameTeamByDirectory))
                return StatusCode(403, new { message = "You are not allowed to edit this worklog." });

            var itemId = _repo.AddItem(worklogId, req.Seq, req.Title, req.Description, req.SpentMinutes, req.ProgressPct, me.UserId);
            return Ok(new { itemId = itemId });
        }
    }
}
