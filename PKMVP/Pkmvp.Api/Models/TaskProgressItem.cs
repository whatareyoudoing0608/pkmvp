using System;

namespace Pkmvp.Api.Models
{
    public class TaskProgressItem
    {
        public decimal ProgressId { get; set; }
        public decimal TaskId { get; set; }
        public decimal AuthorId { get; set; }

        public DateTime LogDate { get; set; }
        public string Status { get; set; }
        public decimal ProgressPct { get; set; }
        public decimal SpentMinutes { get; set; }

        public string CommentTxt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}