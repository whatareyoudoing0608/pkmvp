namespace Pkmvp.Api.Models
{
    public class CreateTaskCommentRequest
    {
        public decimal? ParentCommentId { get; set; }
        public string Content { get; set; }
    }
}
