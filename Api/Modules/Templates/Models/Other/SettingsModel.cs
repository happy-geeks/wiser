using Newtonsoft.Json;

namespace Api.Modules.Templates.Models.Other
{
    public class SettingsModel
    {
        [JsonProperty("prop")]
        public string prop;
        [JsonProperty("val")]
        public object val;
    }
}
