using FrontEnd.Modules.Templates.Models.WtsModels;

namespace FrontEnd.Modules.Templates.Models
{
    public class WtsConfigurationTabViewModel
    {
        public string ServiceName { get; set; }
        public string ConnectionString { get; set; }
        public LogSettings LogSettings { get; set; }
        public string[] LogMinimumLevels { get; set; }
        public string[] RunSchemeTypes { get; set; }
    }
}