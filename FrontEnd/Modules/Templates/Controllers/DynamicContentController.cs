﻿using System;
using System.Collections.Generic;
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
            var viewModel = GenerateDynamicContentInformationViewModel(componentType, data.Data);

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
        /// <returns>
        /// A list of <see cref="TabViewModel"/>.
        /// </returns>
        private static DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, Dictionary<string, object> data)
        {
            var result = new DynamicContentInformationViewModel { Tabs = new List<TabViewModel>() };
            var cmsSettingsType = ReflectionHelper.GetCmsSettingsType(component);
            var orderedProperties = cmsSettingsType.GetProperties().Where(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>() != null).OrderBy(propInfo => propInfo.GetCustomAttribute<CmsPropertyAttribute>()?.DisplayOrder ?? 999);

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
                
                var value = data.FirstOrDefault(setting => property.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase)).Value;
                group.Fields.Add(new FieldViewModel
                {
                    Name = property.Name,
                    CmsPropertyAttribute = cmsPropertyAttribute,
                    PropertyInfo = property,
                    Value = value
                });
            }

            return result;
        }
    }
}
