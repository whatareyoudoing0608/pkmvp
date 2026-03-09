using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace Pkmvp.Api.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        public HealthController(IConfiguration cfg) => _cfg = cfg;

        [HttpGet("db")]
        public async Task<IActionResult> Db()
        {
            var cs = _cfg.GetConnectionString("Oracle");

            using var conn = new OracleConnection(cs);
            await conn.OpenAsync();

            var v = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM DUAL");
            return Ok(new { ok = (v == 1) });
        }
    }
}