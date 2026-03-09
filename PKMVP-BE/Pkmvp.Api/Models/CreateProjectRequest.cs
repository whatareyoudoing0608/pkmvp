namespace Pkmvp.Api.Models
{
    public class CreateProjectRequest
    {
        public string ProjectKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? LeadUserId { get; set; }
    }
}
