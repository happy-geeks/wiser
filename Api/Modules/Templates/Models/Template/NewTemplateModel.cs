using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    public class NewTemplateModel
    {
        public string Name { get; set; }
        public TemplateTypes Type { get; set; }

        public string EditorValue { get; set; }
    }
}
