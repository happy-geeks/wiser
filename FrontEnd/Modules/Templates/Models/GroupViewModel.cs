using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace FrontEnd.Modules.Templates.Models
{
    public class GroupViewModel
    {
        public CmsAttributes.CmsGroupName Name { get; set; }

        public string PrettyName { get; set; }

        public List<FieldViewModel> Fields { get; set; } = new();
    }
}
