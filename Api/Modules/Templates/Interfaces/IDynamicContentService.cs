using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// The service containing the logic needed to use the models in a way the application will be able to process them.
    /// This also forms the link with the dataservice for retrieving data from the database.
    /// </summary>
    public interface IDynamicContentService
    {
        /// <summary>
        /// Retrieve all component modes of a dynamic component.
        /// </summary>
        /// <param name="component">The type of the component from which the modes should be retrieved.</param>
        /// <returns>A dictionary containing the Key and (Display)name for each component mode.</returns>
        Dictionary<int, string> GetComponentModes(Type component);

        /// <summary>
        /// Retrieve all component modes of a dynamic component.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <returns>A list containing the id and name for each component mode.</returns>
        ServiceResult<List<ComponentModeModel>> GetComponentModes(string name);

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
        /// Updates the latest version of a template with new data. This method will overwrite this version, unless this version has been published to the live environment,
        /// then it will create a new version as to not overwrite the live version.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the content to save</param>
        /// <param name="component">A string of the component to match using reflection</param>
        /// <param name="componentMode">An int of the componentMode to match when the modes are retrieved</param>
        /// <param name="title">The name of the template to save</param>
        /// <param name="settings">A dictionary of settings containing their name and value</param>
        /// <returns>The ID of the component.</returns>
        Task<ServiceResult<int>> SaveAsync(ClaimsIdentity identity, int contentId, string component, int componentMode, string title, Dictionary<string, object> settings);

        /// <summary>
        /// Creates a new version of a dynamic component by copying the previous version and increasing the version number by one.
        /// </summary>
        /// <param name="contentId">The content ID of the dynamic component.</param>
        /// <param name="versionBeingDeployed">Optional: If calling this function while deploying the template to an environment, enter the version that is being deployed here. This will then check if that is the latest version and only create a new version of the template if that is the case.</param>
        /// <returns>The ID of the new version.</returns>
        Task<ServiceResult<int>> CreateNewVersionAsync(int contentId, int versionBeingDeployed = 0);

        /// <summary>
        /// Gets the meta data (name, component mode etc) for a component.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="includeSettings">Optional: Whether or not to include the settings that are saved with the component. Default value is <see langword="true" />.</param>
        Task<ServiceResult<DynamicContentOverviewModel>> GetMetaDataAsync(int contentId, bool includeSettings = true);

        /// <summary>
        /// Links a dynamic content to a template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="templateId">The ID of the template.</param>
        Task<ServiceResult<bool>> AddLinkToTemplateAsync(ClaimsIdentity identity, int contentId, int templateId);

        /// <summary>
        /// Get the dynamic component environments. This will retrieve a list of versions and their published environments and convert it to a PublishedEnvironmentModel
        /// containing the Live, accept and test versions and the list of other versions that are present in the data.
        /// </summary>
        /// <param name="contentId">The id of the dynamic component to retrieve the environments of.</param>
        /// <param name="branch">When publishing in a different branch, enter the information for that branch here.</param>
        /// <returns>A model containing the versions that are currently set for the live, accept and test environment.</returns>
        Task<ServiceResult<PublishedEnvironmentModel>> GetEnvironmentsAsync(int contentId, TenantModel branch = null);

        /// <summary>
        /// Publish a dynamic component version to a new environment using a content/component id. This requires you to provide a model with the current published state.
        /// This method will use a generated change log to determine the environments that need to be changed. In some cases publishing an environment will also publish underlaying environments.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the component to publish.</param>
        /// <param name="version">The version of the component to publish.</param>
        /// <param name="environment">The environment to publish the component to.</param>
        /// <param name="currentPublished">A PublishedEnvironmentModel containing the current published templates.</param>
        /// <param name="branch">When publishing in a different branch, enter the information for that branch here.</param>
        Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int contentId, int version, Environments environment, PublishedEnvironmentModel currentPublished, TenantModel branch = null);

        /// <summary>
        /// Duplicate a dynamic component (only the latest version).
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the component.</param>
        /// <param name="newTemplateId">The id of the template to link the new component to.</param>
        Task<ServiceResult<bool>> DuplicateAsync(ClaimsIdentity identity, int contentId, int newTemplateId);

        /// <summary>
        /// Deletes a dynamic component
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the component</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int contentId);

        /// <summary>
        /// Gets all dynamic content that can be linked to the given template.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>A list of dynamic components from other templates.</returns>
        Task<ServiceResult<List<DynamicContentOverviewModel>>> GetLinkableDynamicContentAsync(int templateId);

        /// <summary>
        /// Deploy one or more dynamic contents to a branch.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="dynamicContentIds">The IDs of the dynamic contents to deploy.</param>
        /// <param name="branchId">The ID of the branch to deploy the dynamic contents to.</param>
        Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> dynamicContentIds, int branchId);
    }
}