using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Interfaces
{
    public interface IDynamicContentService
    {
        KeyValuePair<Type, Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> GetAllPropertyAttributes(Type component);
        Task<Dictionary<PropertyInfo, object>> GetCmsSettingsModel(Type component, int templateId);
        Dictionary<object, string> GetComponentModes(Type component);
        Dictionary<TypeInfo, CmsObjectAttribute> GetComponents();
        List<PropertyInfo> GetPropertiesOfType(Type CmsSettingsType);
        Task<int> SaveNewSettings(int templateid, string component, int componentMode, string templateName, Dictionary<string, object> settings);

        Task<List<string>> GetComponentAndModeForContentId(int contentId);
    }
}