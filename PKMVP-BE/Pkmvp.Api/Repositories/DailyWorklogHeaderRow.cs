namespace Pkmvp.Api.Repositories
{
    public class DailyWorklogHeaderRow
    {
        public long WorklogId { get; set; }
        public long ReporterId { get; set; }
        public string ReporterTeamId { get; set; }
        public string Status { get; set; }
    }
}