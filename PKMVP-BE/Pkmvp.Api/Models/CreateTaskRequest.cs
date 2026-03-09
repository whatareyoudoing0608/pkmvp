using System;
using System.Text.Json.Serialization;

namespace Pkmvp.Api.Models
{
    public class CreateTaskRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public int Priority { get; set; } = 3;
        public string Status { get; set; } = "TODO";
        public int ProgressPct { get; set; } = 0;
        public string TaskType { get; set; } = "TASK";

        [JsonIgnore]
        public decimal ReporterId { get; set; }

        public decimal? AssigneeId { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
