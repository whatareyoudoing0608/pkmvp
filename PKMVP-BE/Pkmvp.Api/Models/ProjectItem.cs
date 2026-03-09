using System;

namespace Pkmvp.Api.Models
{
    public class ProjectItem
    {
        public decimal ProjectId { get; set; }
        public string ProjectKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? LeadUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
