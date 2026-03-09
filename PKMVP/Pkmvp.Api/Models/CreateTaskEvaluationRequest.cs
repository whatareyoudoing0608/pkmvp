using System.Text.Json.Serialization;

namespace Pkmvp.Api.Models
{
    public class CreateTaskEvaluationRequest
    {
        [JsonIgnore]
        public decimal EvaluatorId { get; set; }   // 토큰에서 강제 주입

        public int ScoreQuality { get; set; }       // 1~5
        public int ScoreTimeliness { get; set; }    // 1~5
        public int ScoreCommunication { get; set; } // 1~5
        public string CommentTxt { get; set; }
    }
}
