using System;
using System.Text.Json.Serialization;

namespace Pkmvp.Api.Models
{
    public class CreateTaskProgressRequest
    {
        [JsonIgnore]
        public decimal AuthorId { get; set; }      // 토큰에서 강제 주입

        public DateTime? LogDate { get; set; }     // null이면 오늘
        public string Status { get; set; }         // TODO/IN_PROGRESS/BLOCKED/DONE/CANCELED
        public int ProgressPct { get; set; }       // 0~100
        public int SpentMinutes { get; set; } = 0; // 0 이상
        public string CommentTxt { get; set; }
    }
}
