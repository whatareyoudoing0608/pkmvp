using System;

namespace Pkmvp.Api.Models
{
    public class CreateSprintRequest
    {
        public string Name { get; set; }
        public string Goal { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
