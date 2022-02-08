using Api.Modules.Templates.Models.Template;

namespace FrontEnd.Modules.Templates.Models
{
    public class DevelopmentTabViewModel
    {
        public TemplateSettingsModel TemplateSettings { get; set; }
        public LinkedTemplatesModel LinkedTemplates { get; set; }

        public string EditorType { get; set; }
        public string SettingsPartial { get; set; }
    }
}
