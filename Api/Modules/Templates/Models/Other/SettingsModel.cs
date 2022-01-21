using Newtonsoft.Json;

namespace Api.Modules.Templates.Models.Other
{
    public class SettingsModel
    {
        [JsonProperty("prop")]
        public string Prop;
        [JsonProperty("val")]
        public object Val;
    }
}
