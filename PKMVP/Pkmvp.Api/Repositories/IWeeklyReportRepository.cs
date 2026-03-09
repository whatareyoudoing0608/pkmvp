using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pkmvp.Api.Models;

namespace Pkmvp.Api.Repositories
{
    public interface IWeeklyReportRepository
    {
        Task<IReadOnlyList<WeeklySummaryRow>> GetWeeklySummaryAsync(DateTime anyDateInWeek, decimal? userId);
    }
}