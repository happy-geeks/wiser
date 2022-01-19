using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using static GeeksCoreLibrary.Core.Cms.Attributes.CmsAttributes;

namespace Api.Modules.Templates.Services
{
    /// <summary>
    /// The service containing the logic needed to use the models in a way the application will be able to process them. 
    /// This also forms the link with the dataservice for retrieving data from the database.
    /// </summary>
    public class DynamicContentService : IDynamicContentService, IScopedService
    {
        private readonly IDynamicContentDataService dataService;

        public DynamicContentService (IDynamicContentDataService dataService)
        {
            this.dataService = dataService;
        }

        /// <summary>
        /// Get all possible components. These components should are retrieved from the assembly and should have the basetype <c>CmsComponent<CmsSettings, Enum></c>
        /// </summary>
        /// <returns>
        /// Dictionary of typeinfo's and object attributes of all the components found in the GCL.
        /// </returns>
        public Dictionary<TypeInfo, CmsObjectAttribute> GetComponents()
        {
            var componentType = typeof(CmsComponent<CmsSettings, Enum>);
            var assembly = componentType.Assembly;

            var typeInfoList = assembly.DefinedTypes.Where(
                type => type.BaseType != null
                && type.BaseType.IsGenericType
                && componentType.IsGenericType
                && type.BaseType.GetGenericTypeDefinition() == componentType.GetGenericTypeDefinition()
            ).OrderBy(type => type.Name).ToList();

            var resultDictionary = new Dictionary<TypeInfo, CmsObjectAttribute>();

            foreach (var typeInfo in typeInfoList)
            {
                resultDictionary.Add(typeInfo, typeInfo.GetCustomAttribute<CmsObjectAttribute>());
            }
            return resultDictionary;
        }

        /// <summary>
        /// Retrieve the component modes of the current CMScomponent.
        /// </summary>
        /// <param name="component">The type of the component from which the modes should be retrieved.</param>
        /// <returns>
        /// Dictionary containing the Key and (Display)name for each componentmode.
        /// </returns>
        public Dictionary<object, string> GetComponentModes(Type component)
        {
            var info = (component.BaseType).GetTypeInfo();
            var enumtype = info.GetGenericArguments()[1];
            var enumFields = enumtype.GetFields();

            var returnDict = new Dictionary<object, string>();

            foreach (var enumField in enumFields)
            {
                if (enumField.Name.Equals("value__")) continue;
                returnDict.Add(enumField.GetRawConstantValue(), enumField.Name);
            }

            return returnDict;
        }

        /// <summary>
        /// Retrieve the properties of the CMSSettingsmodel.
        /// </summary>
        /// <param name="CmsSettingsType">The CMSSettingsmodel </param>
        /// <returns></returns>
        public List<PropertyInfo> GetPropertiesOfType(Type CmsSettingsType)
        {
            var resultlist = new List<PropertyInfo>();
            var allProperties = CmsSettingsType.GetProperties();

            foreach (var property in allProperties)
            {
                resultlist.Add(property);
            }

            return resultlist;
        }

        /// <summary>
        /// The method retrieves property attributes of a component and will divide the properties into the tabs and groups they belong to.
        /// </summary>
        /// <param name="component">The component from which the properties should be retrieved.</param>
        /// <returns>
        /// Returns Dictionary with the component, tabs, groupsnames and fieldvalues from the type:  
        /// component
        /// (
        ///     Tabname,
        ///     (
        ///         Groupname,
        ///         (
        ///             Propertyname,
        ///             CmsPropertyAttribute
        ///         )
        ///     )
        /// )
        /// </returns>
        public KeyValuePair<Type, Dictionary<CmsTabName, Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> GetAllPropertyAttributes(Type component)
        {
            var resultList = new Dictionary<CmsTabName, Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>();
            var CmsSettingsType = new ReflectionHelper().GetCmsSettingsType(component);
            var propInfos = CmsSettingsType.GetProperties().Where(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>() != null).OrderBy(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>().DisplayOrder);

            foreach (var propInfo in propInfos)
            {
                var cmsPropertyAttribute = propInfo.GetCustomAttribute<CmsPropertyAttribute>();

                if (cmsPropertyAttribute == null)
                {
                    continue;
                }

                if (resultList.ContainsKey(cmsPropertyAttribute.TabName))
                {
                    if (resultList[cmsPropertyAttribute.TabName].ContainsKey(cmsPropertyAttribute.GroupName))
                    {
                        resultList[cmsPropertyAttribute.TabName][cmsPropertyAttribute.GroupName].Add(propInfo, cmsPropertyAttribute);
                    }
                    else
                    {
                        var cmsGroup = CreateCmsGroupFromPropertyAttribute(propInfo, cmsPropertyAttribute);
                        foreach (var kvp in cmsGroup)
                        {
                            resultList[cmsPropertyAttribute.TabName].Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                else
                {
                    resultList.Add(cmsPropertyAttribute.TabName, CreateCmsGroupFromPropertyAttribute(propInfo, cmsPropertyAttribute));
                }
            }
            return new KeyValuePair<Type, Dictionary<CmsTabName, Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>>(component, resultList); ;
        }

        /// <summary>
        /// Creates a new dictionary for properties with the same group.
        /// </summary>
        /// <param name="propName">The name of the property which will be used as the key for the property within the group.</param>
        /// <param name="cmsPropertyAttribute">The CmsAttribute belonging to the attribut within the property group.</param>
        /// <returns>
        /// Dictionary of a cms group. The key is the cmsgroup name and the value is a propertyattribute dictionary.
        /// </returns>
        private Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>> CreateCmsGroupFromPropertyAttribute(PropertyInfo propName, CmsPropertyAttribute cmsPropertyAttribute)
        {
            var cmsGroup = new Dictionary<CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>();
            var propList = new Dictionary<PropertyInfo, CmsPropertyAttribute>();

            propList.Add(propName, cmsPropertyAttribute);
            cmsGroup.Add(cmsPropertyAttribute.GroupName, propList);

            return cmsGroup;
        }

        /// <summary>
        /// Retrieve the settingsmodel with data from the datalayer. This method will couple the data to the corresponding properties.
        /// </summary>
        /// <param name="component">The component to retrieve the properties of.</param>
        /// <returns>
        /// Dictionary with propertyinfo and the value of that property from the data.
        /// </returns>
        public async Task<Dictionary<PropertyInfo, object>> GetCmsSettingsModel(Type component, int templateId)
        {
            var CmsSettingsType = new ReflectionHelper().GetCmsSettingsType(component);
            var properties = GetPropertiesOfType(CmsSettingsType);
            var data = (await dataService.GetTemplateData(templateId)).Value;

            var accountSettings = new Dictionary<PropertyInfo, object>();

            if (data == null)
            {
                return null;
            }

            foreach (var property in properties)
            {
                object value = null;

                //Find matching data
                foreach (var setting in data)
                {
                    if (property.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = setting.Value;
                    }
                }

                accountSettings.Add(property, value);

                if (value == null || value.GetType() == typeof(string) && string.IsNullOrEmpty((string)value))
                {
                }
            }

            return accountSettings;
        }

        /// <summary>
        /// Matches the component using reflection to retrieve its modes and saves the settings.
        /// </summary>
        /// <param name="templateid">The id of the content to save</param>
        /// <param name="component">A string of the component to match using reflection</param>
        /// <param name="componentMode">An int of the componentMode to match when the modes are retrieved</param>
        /// <param name="templateName">The name of the template to save</param>
        /// <param name="settings">A dictionary of settings containing their name and value</param>
        /// <returns>An int as confirmation of the affected rows</returns>
        public async Task<int> SaveNewSettings(int templateid, string component, int componentMode, string templateName, Dictionary<string, object> settings)
        {
            var helper = new ReflectionHelper();
            var modes = GetComponentModes(helper.GetComponentTypeByName(component));
            modes.TryGetValue(componentMode, out var componentModeName);
            return await dataService.SaveSettingsString(templateid, component, componentModeName, templateName, settings);
        }

        /// <summary>
        /// Retrieve the component and componentMode of dynamic content with the given id.
        /// </summary>
        /// <param name="contentId">The id of the dynamic content</param>
        /// <returns>A list of strings containing the componentName and Mode.</returns>
        public async Task<List<string>> GetComponentAndModeForContentId (int contentId)
        {
            var rawComponentAndMode = await dataService.GetComponentAndModeFromContentId(contentId);

            return rawComponentAndMode;
        }
    }
}
