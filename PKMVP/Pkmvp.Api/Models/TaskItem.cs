using System;

namespace Pkmvp.Api.Models
{
    public class TaskItem
    {
        public decimal TaskId { get; set; }
        public string IssueKey { get; set; }
        public string TaskType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Priority { get; set; }
        public string Status { get; set; }
        public decimal ProgressPct { get; set; }

        public decimal ReporterId { get; set; }
        public decimal? AssigneeId { get; set; }
        public decimal? ProjectId { get; set; }
        public decimal? SprintId { get; set; }
        public decimal? StoryPoints { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? DoneDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
