using System;

namespace Pkmvp.Api.Models
{
    public class SprintItem
    {
        public decimal SprintId { get; set; }
        public decimal BoardId { get; set; }
        public string Name { get; set; }
        public string Goal { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
