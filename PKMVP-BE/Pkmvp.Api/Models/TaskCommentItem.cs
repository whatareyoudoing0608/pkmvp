using System;

namespace Pkmvp.Api.Models
{
    public class TaskCommentItem
    {
        public decimal CommentId { get; set; }
        public decimal TaskId { get; set; }
        public decimal? ParentCommentId { get; set; }
        public decimal AuthorId { get; set; }
        public string Content { get; set; }
        public string EditedYn { get; set; }
        public string DeletedYn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
