using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace FrontEnd.Modules.Templates.Models;

public class TabViewModel
{
    public CmsAttributes.CmsTabName Name { get; set; }

    public string PrettyName { get; set; }

    public List<GroupViewModel> Groups { get; set; } = [];
}