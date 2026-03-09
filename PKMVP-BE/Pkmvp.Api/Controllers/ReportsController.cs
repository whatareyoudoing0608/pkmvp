using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Pkmvp.Api.Repositories;

namespace Pkmvp.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IWeeklyReportRepository _repo;

        public ReportsController(IWeeklyReportRepository repo)
        {
            _repo = repo;
        }

        // GET /api/reports/weekly?weekDate=2026-02-27
        [HttpGet("weekly")]
        public async Task<IActionResult> Weekly([FromQuery] DateTime weekDate, [FromQuery] decimal? userId = null)
        {
            var rows = await _repo.GetWeeklySummaryAsync(weekDate, userId);
            return Ok(rows);
        }
    }
}