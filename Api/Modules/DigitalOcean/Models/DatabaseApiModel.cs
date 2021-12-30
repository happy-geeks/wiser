using System.Collections.Generic;
using Newtonsoft.Json;

namespace Api.Modules.DigitalOcean.Models
{
    public class DatabaseApiModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Engine { get; set; }
        public string Version { get; set; }
        public string Region { get; set; }
        [JsonProperty("db_names")]
        public List<string> DatabaseNames { get; set; }
    }
}
