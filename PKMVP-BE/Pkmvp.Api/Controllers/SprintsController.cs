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
    [Route("api/sprints")]
    public class SprintsController : ControllerBase
    {
        private readonly IPlanningRepository _planning;
        private readonly ICurrentUserAccessor _cu;

        public SprintsController(IPlanningRepository planning, ICurrentUserAccessor cu)
        {
            _planning = planning;
            _cu = cu;
        }

        [HttpPatch("{sprintId}/status")]
        public async Task<IActionResult> UpdateStatus(decimal sprintId, [FromBody] UpdateSprintStatusRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Status))
                return BadRequest("status required");

            var me = _cu.Get();
            var deny = EnsureCanManage(me);
            if (deny != null) return deny;

            var status = req.Status.Trim().ToUpperInvariant();
            if (status != "PLANNED" && status != "ACTIVE" && status != "CLOSED")
                return BadRequest("status must be PLANNED, ACTIVE, or CLOSED");

            var existing = await _planning.GetSprintAsync(sprintId);
            if (existing == null) return NotFound();

            await _planning.UpdateSprintStatusAsync(sprintId, status);
            var updated = await _planning.GetSprintAsync(sprintId);
            return Ok(updated);
        }

        private IActionResult EnsureCanManage(CurrentUser me)
        {
            if (me.Role == UserRole.ADMIN || me.Role == UserRole.MANAGER)
                return null;

            return StatusCode(403, new { message = "Manager or Admin role required." });
        }
    }
}
