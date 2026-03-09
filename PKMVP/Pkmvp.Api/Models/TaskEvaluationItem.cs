using System;

namespace Pkmvp.Api.Models
{
    public class TaskEvaluationItem
    {
        public decimal EvalId { get; set; }
        public decimal TaskId { get; set; }
        public decimal EvaluatorId { get; set; }

        public decimal ScoreQuality { get; set; }
        public decimal ScoreTimeliness { get; set; }
        public decimal ScoreCommunication { get; set; }

        public string CommentTxt { get; set; }
        public DateTime EvaluatedAt { get; set; }
    }
}