using GeeksCoreLibrary.Modules.Templates.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.Templates.Models.Template
{
    public class NewTemplateModel
    {
        public string name { get; set; }
        public TemplateTypes type { get; set; }

        public string editorValue { get; set; }
    }
}
