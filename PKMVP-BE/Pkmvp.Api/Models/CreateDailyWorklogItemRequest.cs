namespace Pkmvp.Api.Models
{
    public class CreateDailyWorklogItemRequest
    {
        public int Seq { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? SpentMinutes { get; set; }
        public int? ProgressPct { get; set; }
    }
}