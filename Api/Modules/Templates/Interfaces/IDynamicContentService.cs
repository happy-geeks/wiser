using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.DynamicContent;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// The service containing the logic needed to use the models in a way the application will be able to process them. 
    /// This also forms the link with the dataservice for retrieving data from the database.
    /// </summary>
    public interface IDynamicContentService
    {
        /// <summary>
        /// Retrieve the component modes of the current CMScomponent.
        /// </summary>
        /// <param name="component">The type of the component from which the modes should be retrieved.</param>
        /// <returns>
        /// Dictionary containing the Key and (Display)name for each componentmode.
        /// </returns>
        Dictionary<int, string> GetComponentModes(Type component);
        
        /// <summary>
        /// Retrieve the component modes of the current CMScomponent.
        /// </summary>
        /// <param name="componentType">The name of the type.</param>
        /// <returns>
        /// Dictionary containing the Key and (Display)name for each componentmode.
        /// </returns>
        ServiceResult<List<ComponentModeModel>> GetComponentModes(string componentType);

        /// <summary>
        /// Retrieve the properties of the CMSSettingsmodel.
        /// </summary>
        /// <param name="cmsSettingsType">The CMSSettingsmodel </param>
        List<PropertyInfo> GetPropertiesOfType(Type cmsSettingsType);

        /// <summary>
        /// Retrieve the settingsmodel with data from the datalayer. This method will couple the data to the corresponding properties.
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns>
        /// Dictionary with propertyinfo and the value of that property from the data.
        /// </returns>
        Task<ServiceResult<Dictionary<string, object>>> GetComponentDataAsync(int contentId);

        /// <summary>
        /// Matches the component using reflection to retrieve its modes and saves the settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the content to save</param>
        /// <param name="component">A string of the component to match using reflection</param>
        /// <param name="componentMode">An int of the componentMode to match when the modes are retrieved</param>
        /// <param name="title">The name of the template to save</param>
        /// <param name="settings">A dictionary of settings containing their name and value</param>
        /// <returns>An int with the new ID</returns>
        Task<ServiceResult<int>> SaveNewSettingsAsync(ClaimsIdentity identity, int contentId, string component, int componentMode, string title, Dictionary<string, object> settings);

        /// <summary>
        /// Gets the meta data (name, component mode etc) for a component.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <returns></returns>
        Task<ServiceResult<DynamicContentOverviewModel>> GetMetaDataAsync(int contentId);
        
        /// <summary>
        /// Links a dynamic content to a template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="templateId">The ID of the template.</param>
        Task<ServiceResult<bool>> AddLinkToTemplateAsync(ClaimsIdentity identity, int contentId, int templateId);
    }
}