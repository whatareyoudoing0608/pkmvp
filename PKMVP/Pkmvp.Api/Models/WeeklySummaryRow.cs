using System;

namespace Pkmvp.Api.Models
{
    public class WeeklySummaryRow
    {
        public decimal UserId { get; set; }
        public string UserName { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public decimal SumMinutes { get; set; }
        public decimal DoneCnt { get; set; }
        public decimal AvgScore { get; set; }
        public decimal BlockedCnt { get; set; }
    }
}