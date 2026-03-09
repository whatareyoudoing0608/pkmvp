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
    [Route("api/boards")]
    public class BoardsController : ControllerBase
    {
        private readonly IPlanningRepository _planning;
        private readonly ICurrentUserAccessor _cu;

        public BoardsController(IPlanningRepository planning, ICurrentUserAccessor cu)
        {
            _planning = planning;
            _cu = cu;
        }

        [HttpGet("{boardId}/sprints")]
        public async Task<IActionResult> ListSprints(decimal boardId)
        {
            var board = await _planning.GetBoardAsync(boardId);
            if (board == null) return NotFound();

            var rows = await _planning.ListSprintsByBoardAsync(boardId);
            return Ok(rows);
        }

        [HttpPost("{boardId}/sprints")]
        public async Task<IActionResult> CreateSprint(decimal boardId, [FromBody] CreateSprintRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("name required");

            var me = _cu.Get();
            var deny = EnsureCanManage(me);
            if (deny != null) return deny;

            var board = await _planning.GetBoardAsync(boardId);
            if (board == null) return NotFound();

            req.Name = req.Name.Trim();
            req.Goal = req.Goal != null ? req.Goal.Trim() : null;

            if (req.Name.Length > 200) return BadRequest("name max length is 200");

            var id = await _planning.CreateSprintAsync(boardId, req);
            var created = await _planning.GetSprintAsync(id);
            return Created($"/api/sprints/{id}", created);
        }

        [HttpGet("{boardId}/issues")]
        public async Task<IActionResult> ListIssues(decimal boardId, [FromQuery] decimal? sprintId = null, [FromQuery] string status = null)
        {
            var board = await _planning.GetBoardAsync(boardId);
            if (board == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(status) && !TaskStatusGuard.IsAllowed(status))
                return BadRequest("Invalid status. Allowed: " + TaskStatusGuard.AllowedList());

            if (sprintId.HasValue)
            {
                var sprint = await _planning.GetSprintAsync(sprintId.Value);
                if (sprint == null || sprint.BoardId != boardId)
                    return BadRequest("sprintId is not valid for this board");
            }

            var rows = await _planning.ListBoardIssuesAsync(boardId, sprintId, status);
            return Ok(rows);
        }

        [HttpPost("{boardId}/issues/{taskId}/plan")]
        public async Task<IActionResult> PlanIssue(decimal boardId, decimal taskId, [FromBody] PlanBoardIssueRequest req)
        {
            req = req ?? new PlanBoardIssueRequest();

            var me = _cu.Get();
            var deny = EnsureCanManage(me);
            if (deny != null) return deny;

            var board = await _planning.GetBoardAsync(boardId);
            if (board == null) return NotFound();

            if (req.SprintId.HasValue)
            {
                var sprint = await _planning.GetSprintAsync(req.SprintId.Value);
                if (sprint == null || sprint.BoardId != boardId)
                    return BadRequest("sprintId is not valid for this board");
            }

            var ok = await _planning.PlanIssueAsync(boardId, taskId, req.SprintId);
            if (!ok) return BadRequest("Failed to plan issue. Check taskId and sprintId.");

            return Ok(new { ok = true });
        }

        private IActionResult EnsureCanManage(CurrentUser me)
        {
            if (me.Role == UserRole.ADMIN || me.Role == UserRole.MANAGER)
                return null;

            return StatusCode(403, new { message = "Manager or Admin role required." });
        }
    }
}
