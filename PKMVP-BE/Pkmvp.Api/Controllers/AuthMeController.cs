using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkmvp.Api.Auth;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/auth")]
    public class AuthMeController : ControllerBase
    {
        private readonly ICurrentUserAccessor _cu;

        public AuthMeController(ICurrentUserAccessor cu)
        {
            _cu = cu;
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var me = _cu.Get();
            return Ok(new { me.UserId, role = me.Role.ToString(), me.TeamId });
        }
    }
}