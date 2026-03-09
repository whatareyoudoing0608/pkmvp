using System;

namespace Pkmvp.Api.Models
{
    public class BoardItem
    {
        public decimal BoardId { get; set; }
        public decimal ProjectId { get; set; }
        public string Name { get; set; }
        public string BoardType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
