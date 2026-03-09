using System.Text.RegularExpressions;
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
    [Route("api/projects")]
    public class ProjectsController : ControllerBase
    {
        private static readonly Regex ProjectKeyRegex = new Regex(@"^[A-Z][A-Z0-9_-]{1,19}$", RegexOptions.Compiled);

        private readonly IPlanningRepository _planning;
        private readonly ICurrentUserAccessor _cu;

        public ProjectsController(IPlanningRepository planning, ICurrentUserAccessor cu)
        {
            _planning = planning;
            _cu = cu;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var rows = await _planning.ListProjectsAsync();
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.ProjectKey)) return BadRequest("projectKey required");
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("name required");

            var me = _cu.Get();
            var deny = EnsureCanManage(me);
            if (deny != null) return deny;

            req.ProjectKey = req.ProjectKey.Trim().ToUpperInvariant();
            req.Name = req.Name.Trim();
            req.Description = req.Description != null ? req.Description.Trim() : null;

            if (!ProjectKeyRegex.IsMatch(req.ProjectKey))
                return BadRequest("projectKey must match ^[A-Z][A-Z0-9_\\-]{1,19}$");

            if (req.Name.Length > 200) return BadRequest("name max length is 200");

            var id = await _planning.CreateProjectAsync(req);
            var created = await _planning.GetProjectAsync(id);
            return Created($"/api/projects/{id}", created);
        }

        [HttpGet("{projectId}/boards")]
        public async Task<IActionResult> ListBoards(decimal projectId)
        {
            var project = await _planning.GetProjectAsync(projectId);
            if (project == null) return NotFound();

            var rows = await _planning.ListBoardsByProjectAsync(projectId);
            return Ok(rows);
        }

        [HttpPost("{projectId}/boards")]
        public async Task<IActionResult> CreateBoard(decimal projectId, [FromBody] CreateBoardRequest req)
        {
            if (req == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("name required");

            var me = _cu.Get();
            var deny = EnsureCanManage(me);
            if (deny != null) return deny;

            var project = await _planning.GetProjectAsync(projectId);
            if (project == null) return NotFound();

            req.Name = req.Name.Trim();
            req.BoardType = string.IsNullOrWhiteSpace(req.BoardType) ? "KANBAN" : req.BoardType.Trim().ToUpperInvariant();

            if (req.Name.Length > 200) return BadRequest("name max length is 200");
            if (req.BoardType != "KANBAN" && req.BoardType != "SCRUM")
                return BadRequest("boardType must be KANBAN or SCRUM");

            var id = await _planning.CreateBoardAsync(projectId, req);
            var created = await _planning.GetBoardAsync(id);
            return Created($"/api/boards/{id}", created);
        }

        private IActionResult EnsureCanManage(CurrentUser me)
        {
            if (me.Role == UserRole.ADMIN || me.Role == UserRole.MANAGER)
                return null;

            return StatusCode(403, new { message = "Manager or Admin role required." });
        }
    }
}

