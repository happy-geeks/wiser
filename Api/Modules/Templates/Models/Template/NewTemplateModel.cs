using GeeksCoreLibrary.Modules.Templates.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.Templates.Models.Template
{
    public class NewTemplateModel
    {
        public string Name { get; set; }
        public TemplateTypes Type { get; set; }

        public string EditorValue { get; set; }
    }
}
