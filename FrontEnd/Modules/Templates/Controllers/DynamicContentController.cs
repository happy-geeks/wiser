using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Templates.Controllers
{
    [Area("Templates"), Route("Modules/DynamicContent")]
    public class DynamicContentController : Controller
    {
        private readonly IBaseService baseService;

        public DynamicContentController(IBaseService baseService)
        {
            this.baseService = baseService;
        }

        /// <summary>
        /// The main page on starting the application. TODO: open with the current component as original
        /// </summary>
        /// <returns>
        /// View containing the attributes of the component to be displayed as fields.
        /// </returns>
        [HttpGet, Route("{id:int}")]
        public IActionResult Index(int id)
        {
            var viewModel = baseService.CreateBaseViewModel<DynamicContentViewModel>();
            viewModel.ContentId = id;
            viewModel.Components = GetComponents();
            return View(viewModel);
        }

        /// <summary>
        /// Get The tabwindow containing the component properties.
        /// </summary>
        /// <param name="component">The component which properties should be loaded into the tab window.</param>
        /// <param name="data">The data of the component.</param>
        /// <returns>The Partial view containing the HTML of the tab window from dynamic content.</returns>
        [HttpPost, Route("{component}/DynamicContentTabPane")]
        public IActionResult DynamicContentTabPane(string component, [FromBody]DynamicContentOverviewModel data)
        {
            var componentType = ReflectionHelper.GetComponentTypeByName(component);
            var propertyAttributes = GetAllPropertyAttributes(componentType);

            var viewModel = new DynamicContentInformationModel
            {
                PropertyAttributes = propertyAttributes,
                PropValues = GetCmsSettingsModelAsync(component, data.Data)
            };

            return PartialView("Partials/DynamicContentTabPane", viewModel);
        }

        [HttpPost, Route("History")]
        public IActionResult History([FromBody]List<HistoryVersionModel> viewData)
        {
            return PartialView("Partials/DynamicContentHistoryPane", viewData);
        }

        
        /// <summary>
        /// Get all possible components. These components should are retrieved from the assembly and should have the basetype CmsComponent&lt;CmsSettings, Enum&gt;
        /// </summary>
        /// <returns>Dictionary of typeinfos and object attributes of all the components found in the GCL.</returns>
        private Dictionary<TypeInfo, CmsObjectAttribute> GetComponents()
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
        private KeyValuePair<Type, Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> GetAllPropertyAttributes(Type component)
        {
            var resultList = new Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>();
            var CmsSettingsType = ReflectionHelper.GetCmsSettingsType(component);
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
            return new KeyValuePair<Type, Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>>(component, resultList); ;
        }

        /// <summary>
        /// Creates a new dictionary for properties with the same group.
        /// </summary>
        /// <param name="propName">The name of the property which will be used as the key for the property within the group.</param>
        /// <param name="cmsPropertyAttribute">The CmsAttribute belonging to the attribut within the property group.</param>
        /// <returns>
        /// Dictionary of a cms group. The key is the cmsgroup name and the value is a propertyattribute dictionary.
        /// </returns>
        private Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>> CreateCmsGroupFromPropertyAttribute(PropertyInfo propName, CmsPropertyAttribute cmsPropertyAttribute)
        {
            var cmsGroup = new Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>();
            var propList = new Dictionary<PropertyInfo, CmsPropertyAttribute>();

            propList.Add(propName, cmsPropertyAttribute);
            cmsGroup.Add(cmsPropertyAttribute.GroupName, propList);

            return cmsGroup;
        }

        private Dictionary<PropertyInfo, object> GetCmsSettingsModelAsync(string componentName, Dictionary<string, object> data)
        {
            var component = ReflectionHelper.GetComponentTypeByName(componentName);
            var cmsSettingsType = ReflectionHelper.GetCmsSettingsType(component);
            var properties = GetPropertiesOfType(cmsSettingsType);

            var accountSettings = new Dictionary<PropertyInfo, object>();

            if (data == null)
            {
                return null;
            }

            foreach (var property in properties)
            {
                //Find matching data
                var setting = data.FirstOrDefault(setting => property.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase));
                var value = setting.Value;

                accountSettings.Add(property, value);
            }

            return accountSettings;
        }

        private List<PropertyInfo> GetPropertiesOfType(Type cmsSettingsType)
        {
            var resultList = new List<PropertyInfo>();
            var allProperties = cmsSettingsType.GetProperties();

            foreach (var property in allProperties)
            {
                resultList.Add(property);
            }

            return resultList;
        }
    }
}
