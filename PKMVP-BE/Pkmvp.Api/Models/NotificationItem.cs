using System;

namespace Pkmvp.Api.Models
{
    public class NotificationItem
    {
        public decimal NotificationId { get; set; }
        public decimal UserId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string TargetType { get; set; }
        public decimal? TargetId { get; set; }
        public string IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
