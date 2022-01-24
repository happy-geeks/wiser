using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using GeeksCoreLibrary.Components.Account;
using GeeksCoreLibrary.Core.Cms.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/dynamic-content"), ApiController, Authorize]
    public class DynamicContentController : Controller
    {/*
        private readonly IDynamicContentService dynamicContentService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentController"/>.
        /// </summary>
        public DynamicContentController(IDynamicContentService dynamicContentService)
        {
            this.dynamicContentService = dynamicContentService;
        }

        public async Task<IActionResult> Get(int id)
        {
            var propertyAttributes = dynamicContentService.GetAllPropertyAttributes(typeof(Account));

            var dcInformation = new DynamicContentInformationModel
            {
                Components = dynamicContentService.GetComponents(),
                ComponentModes = dynamicContentService.GetComponentModes(typeof(Account)),
                PropertyAttributes = propertyAttributes,
                PropValues = await dynamicContentService.GetCmsSettingsModel(propertyAttributes.Key, id)
            };

            return View(new DynamicContentInformationModel());
        }

        /// <summary>
        /// Get The tabwindow containing the component properties.
        /// </summary>
        /// <param name="component">The component which properties should be loaded into the tab window.</param>
        /// <returns>The Partial view containing the HTML of the tab window from dynamic content.</returns>
        [HttpGet]
        public async Task<IActionResult> DynamicContentTabPane(int templateId, string component)
        {
            var helper = new ReflectionHelper();
            var componentType = helper.GetComponentTypeByName(component);
            var propertyAttributes = dynamicContentService.GetAllPropertyAttributes(componentType);

            var dcInformation = new DynamicContentInformationModel
            {
                Components = dynamicContentService.GetComponents(),
                ComponentModes = dynamicContentService.GetComponentModes(typeof(Account)),
                PropertyAttributes = propertyAttributes,
                PropValues = await dynamicContentService.GetCmsSettingsModel(propertyAttributes.Key, templateId)
            };

            return PartialView("Partials/DynamicContentTabPane", dcInformation);
        }


        /// <summary>
        /// Get the componentModeOptions from the given component.
        /// </summary>
        /// <param name="component">The component of which the modes need to be retrieved.</param>
        /// <returns>The Partial view containing the HTML of the component modes.</returns>
        [HttpGet]
        public IActionResult ReloadComponentModeOptions(string component)
        {
            var helper = new ReflectionHelper();
            var componentType = helper.GetComponentTypeByName(component);

            return PartialView("Partials/ComponentModeOptions", GetComponentModes(componentType));
        }

        /// <summary>
        /// Retrieve properties for the initial load of the coponent tab.
        /// </summary>
        /// <returns>A Keyvalue pair containing the type, tabs, groups and properties of the account component.</returns>
        public KeyValuePair<Type, Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> GetPropertyAttributes()
        {
            return dynamicContentService.GetAllPropertyAttributes(typeof(Account));
        }

        /// <summary>
        /// Method for retrieving the saved settings from a component.
        /// </summary>
        /// <returns>
        /// Returns a dictionary with the propertyinfo and value(as object) of each setting.
        /// </returns>
        public async Task<Dictionary<PropertyInfo, object>> GetPropertyValues(Type component, int templateId)
        {
            var settingValues = await dynamicContentService.GetCmsSettingsModel(component, templateId);
            return settingValues;
        }

        /// <summary>
        /// Gets all possible components from the GCL.
        /// </summary>
        /// <returns>A Dictionary returning the TypeInfo and CmsAttributeObject of all the components.</returns>
        public Dictionary<TypeInfo, CmsObjectAttribute> GetComponents()
        {
            return dynamicContentService.GetComponents();
        }

        /// <summary>
        /// Retrieve the componentmodes of the Component. TODO: make the component variable
        /// </summary>
        /// <returns>
        /// Dictionary containing the component Key and (Display)name.
        /// </returns>
        public Dictionary<object, string> GetComponentModes()
        {
            return dynamicContentService.GetComponentModes(typeof(Account));
        }

        /// <summary>
        /// Retrieve the componentmodes of the Component.
        /// </summary>
        /// <returns>
        /// Dictionary containing the component Key and (Display)name.
        /// </returns>
        public Dictionary<object, string> GetComponentModes(Type component)
        {
            return dynamicContentService.GetComponentModes(component);
        }

        /// <summary>
        ///  POST endpoint for saving the settings of a component.
        /// </summary>
        /// <param name="templateId">The id of the content to save</param>
        /// <param name="component">A string of the component of the content. This will be checked using reflection.</param>
        /// <param name="componentMode">An int representing the mode the component is in</param>
        /// <param name="template_name">The name of the template</param>
        /// <param name="settings">A json containing a dictionary of the settings of the content.</param>
        /// <returns>A list of saved settings as confirmation.</returns>
        [HttpPost]
        public async Task<string> SaveSettings(int templateId, string component, int componentMode, string template_name, string settings)
        {
            if (templateId == 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (String.IsNullOrEmpty(component))
            {
                throw new ArgumentException("The component is incorrect.");
            }
            if (componentMode == 0)
            {
                throw new ArgumentException("The componentMode cannot be zero.");
            }
            var list = JsonConvert.DeserializeObject<Dictionary<string, object>>(settings);
            await dynamicContentService.SaveNewSettings(templateId, component, componentMode, template_name, list);

            return JsonConvert.SerializeObject(list);
        }

        /// <summary>
        /// GET endpoint for getting the component modes of a certain component.
        /// </summary>
        /// <param name="component">A string of the component of the content. This will be checked using reflection.</param>
        /// <returns>
        /// JSON list of componentmodes. The JSON contains a formatted dictionary containing the key and (Display)name for all components.
        /// </returns>
        [HttpGet]
        public JsonResult GetComponentModesAsJsonResult(string component)
        {
            if (String.IsNullOrEmpty(component))
            {
                throw new ArgumentException("The component is incorrect.");
            }
            var helper = new ReflectionHelper();
            var componentType = helper.GetComponentTypeByName(component);

            return Json(GetComponentModes(componentType));
        }

        /// <summary>
        /// Get the history of the current component.
        /// </summary>
        /// <param name="component">The component of the history.</param>
        /// <returns>History PartialView containing the retrieved history of the component</returns>
        [HttpGet]
        public async Task<IActionResult> GetHistoryOfComponent(int templateId)
        {
            if (templateId == 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return PartialView("Partials/DynamicContentHistoryPane", await historyService.GetChangesInComponent(templateId));
        }


        /// <summary>
        /// Undo changes that have been made.
        /// </summary>
        /// <param name="changes">A json string of changes that can be converted to a List of RevertHistoryModels</param>
        /// <returns>An int representing the affected rows as confirmation</returns>
        [HttpPost]
        public async Task<int> UndoChanges(string changes, int templateId)
        {
            if (templateId == 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (String.IsNullOrEmpty(changes))
            {
                throw new ArgumentException("The content cannot be reverted without changes.");
            }
            return await historyService.RevertChanges(JsonConvert.DeserializeObject<List<RevertHistoryModel>>(changes), templateId);
        }

        /// <summary>
        /// Retrieves the component and componentMode of a dynamic content item through its id.
        /// </summary>
        /// <param name="contentId">The id of the dynamic content</param>
        /// <returns>A list of strings containing the component and componentMode</returns>
        [HttpGet]
        public async Task<List<string>> GetComponentAndModeForContentId(int contentId)
        {
            if (contentId == 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await dynamicContentService.GetComponentAndModeForContentId(contentId);
        }*/
    }
}
