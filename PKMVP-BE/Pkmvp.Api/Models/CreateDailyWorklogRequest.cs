using System;

namespace Pkmvp.Api.Models
{
    public class CreateDailyWorklogRequest
    {
        public DateTime WorkDate { get; set; }
        public string Summary { get; set; }
    }
}