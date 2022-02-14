using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        public IActionResult Index(int id, int templateId)
        {
            var viewModel = baseService.CreateBaseViewModel<DynamicContentViewModel>();
            viewModel.ContentId = id;
            viewModel.TemplateId = templateId;
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
            var viewModel = GenerateDynamicContentInformationViewModel(componentType, data.Data, data.ComponentMode);

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
        /// <param name="data">The data from database for this component.</param>
        /// <param name="componentMode">The selected mode of the component.</param>
        /// <returns>
        /// A list of <see cref="TabViewModel"/>.
        /// </returns>
        private static DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, Dictionary<string, object> data, string componentMode)
        {
            var cmsSettingsType = ReflectionHelper.GetCmsSettingsType(component);
            return GenerateDynamicContentInformationViewModel(component, cmsSettingsType.GetProperties(), data, componentMode);
        }

        /// <summary>
        /// The method retrieves property attributes of a component and will divide the properties into the tabs and groups they belong to.
        /// </summary>
        /// <param name="component">The component type.</param>
        /// <param name="properties">The properties of the component.</param>
        /// <param name="data">The data from database for this component.</param>
        /// <param name="componentMode">The selected mode of the component.</param>
        /// <returns>
        /// A list of <see cref="TabViewModel"/>.
        /// </returns>
        private static DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, IEnumerable<PropertyInfo> properties, Dictionary<string, object> data, string componentMode)
        {
            var result = new DynamicContentInformationViewModel { Tabs = new List<TabViewModel>() };
            var orderedProperties = properties.Where(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>() != null).OrderBy(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>()?.DisplayOrder ?? 999);

            foreach (var property in orderedProperties)
            {
                var cmsPropertyAttribute = property.GetCustomAttribute<CmsPropertyAttribute>();
                
                if (cmsPropertyAttribute == null || cmsPropertyAttribute.HideInCms)
                {
                    continue;
                }

                var tabEnumAttribute = cmsPropertyAttribute.TabName.GetAttributeOfType<CmsEnumAttribute>();
                var tab = result.Tabs.SingleOrDefault(t => t.Name == cmsPropertyAttribute.TabName);
                if (tab == null)
                {
                    tab = new TabViewModel
                    {
                        Name = cmsPropertyAttribute.TabName,
                        PrettyName = tabEnumAttribute?.PrettyName ?? cmsPropertyAttribute.TabName.ToString(),
                        Groups = new List<GroupViewModel>()
                    };

                    result.Tabs.Add(tab);
                }
                
                var groupEnumAttribute = cmsPropertyAttribute.GroupName.GetAttributeOfType<CmsEnumAttribute>();
                var group = tab.Groups.SingleOrDefault(g => g.Name == cmsPropertyAttribute.GroupName);
                if (group == null)
                {
                    group = new GroupViewModel
                    {
                        Name = cmsPropertyAttribute.GroupName,
                        PrettyName = groupEnumAttribute?.PrettyName ?? cmsPropertyAttribute.GroupName.ToString(),
                        Fields = new List<FieldViewModel>()
                    };

                    tab.Groups.Add(group);
                }

                var isDefaultValue = false;
                var value = data.FirstOrDefault(setting => property.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase)).Value;

                // If we have no value, get the default value.
                if (component != null && (value == property.PropertyType.GetDefaultValue() || (value is string stringValue && String.IsNullOrEmpty(stringValue))))
                {
                    var assembly = Assembly.GetAssembly(component);
                    var fullTypeName = $"{component.Namespace}.Models.{component.Name}{componentMode}SettingsModel";
                    var type = assembly?.GetType(fullTypeName);
                    var defaultValueProperty = type?.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(p => p.Name == property.Name);
                    var defaultValueAttribute = defaultValueProperty?.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValueAttribute != null)
                    {
                        value = defaultValueAttribute.Value;
                        isDefaultValue = true;
                    }
                }

                var field = new FieldViewModel
                {
                    Name = property.Name,
                    CmsPropertyAttribute = cmsPropertyAttribute,
                    PropertyInfo = property,
                    Value = value,
                    IsDefaultValue = isDefaultValue
                };
                group.Fields.Add(field);

                // Get sub values if we have any. (For example, a Repeater component has a dynamic amount of layers, this code handles that functionality.)
                if (property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(SortedList<,>) || property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && property.PropertyType.GetGenericArguments().First() == typeof(string))
                {
                    var subSettingsType = property.PropertyType.GetGenericArguments().Last();

                    var subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), new Dictionary<string, object>(), componentMode);
                    field.SubFields = new Dictionary<string, List<FieldViewModel>>
                    {
                        { "_template", subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList() }
                    };
                    
                    if (value is JObject { HasValues: true } jsonObject)
                    {
                        foreach (var item in jsonObject.Properties())
                        {
                            subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), item.Value.ToObject<Dictionary<string, object>>(), componentMode);
                            field.SubFields ??= new Dictionary<string, List<FieldViewModel>>();
                            field.SubFields.Add(item.Name, subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList());
                        }
                    }
                    else
                    {
                        subData = GenerateDynamicContentInformationViewModel(component, subSettingsType.GetProperties(), new Dictionary<string, object>(), componentMode);
                        field.SubFields.Add("", subData.Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Fields)).ToList());
                    }
                }
            }

            return result;
        }
    }
}
