using System;
using System.Collections.Generic;
using System.Reflection;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Modules.Templates.ViewModels;
using static GeeksCoreLibrary.Core.Cms.Attributes.CmsAttributes;

namespace Api.Modules.Templates.Models.DynamicContent
{
    public class DynamicContentInformationModel : PageViewModel
    {
        public Dictionary<TypeInfo, CmsObjectAttribute> components { get; set; }
        public Dictionary<object, string> componentModes { get; set; }
        public KeyValuePair<Type, Dictionary<CmsTabName, Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> propertyAttributes { get; set; }
        public Dictionary<PropertyInfo, object> propValues { get; set; }
    }
}
