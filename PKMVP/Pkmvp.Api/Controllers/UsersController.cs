using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Pkmvp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UsersController(IConfiguration config)
        {
            _config = config;
        }

        // GET /api/users?q=...&teamId=...
        // - appsettings.json(Auth:Users) 기반의 간단 디렉토리(비밀번호 제외)
        [HttpGet]
        public IActionResult List([FromQuery] string q = null, [FromQuery] string teamId = null)
        {
            var users = _config.GetSection("Auth:Users").GetChildren()
                .Select(x => new
                {
                    userId = long.TryParse(x["UserId"], out var uid) ? uid : 0,
                    displayName = x["LoginId"],
                    teamId = x["TeamId"],
                    role = x["Role"]
                })
                .Where(x => x.userId > 0)
                .ToList();

            if (!string.IsNullOrWhiteSpace(teamId))
            {
                users = users
                    .Where(x => string.Equals(x.teamId, teamId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();
                users = users
                    .Where(x =>
                        (x.displayName ?? "").Contains(qq, StringComparison.OrdinalIgnoreCase)
                        || (x.teamId ?? "").Contains(qq, StringComparison.OrdinalIgnoreCase)
                        || x.userId.ToString().Contains(qq, StringComparison.OrdinalIgnoreCase)
                        || (x.role ?? "").Contains(qq, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            return Ok(users);
        }
    }
}
