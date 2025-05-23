﻿using System.Collections.Generic;
using System.Reflection;
using FrontEnd.Modules.Base.Models;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace FrontEnd.Modules.Templates.Models;

public class DynamicContentViewModel : BaseModuleViewModel
{
    /// <summary>
    /// Gets or sets the ID of the component.
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the template from which this component was opened.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Set to make dynamic content page switch to tab on load
    /// </summary>
    public string InitialTab { get; set; }

    /// <summary>
    /// Gets or sets a list of all available components.
    /// </summary>
    public Dictionary<TypeInfo, CmsObjectAttribute> Components { get; set; }
}