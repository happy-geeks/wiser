using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
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
        /// Get all possible components. These components should are retrieved from the assembly and should have the basetype CmsComponent&lt;CmsSettings, Enum&gt;
        /// </summary>
        /// <returns>Dictionary of typeinfos and object attributes of all the components found in the GCL.</returns>
        Dictionary<TypeInfo, CmsObjectAttribute> GetComponents();

        /// <summary>
        /// Retrieve the component modes of the current CMScomponent.
        /// </summary>
        /// <param name="component">The type of the component from which the modes should be retrieved.</param>
        /// <returns>
        /// Dictionary containing the Key and (Display)name for each componentmode.
        /// </returns>
        Dictionary<object, string> GetComponentModes(Type component);

        /// <summary>
        /// Retrieve the properties of the CMSSettingsmodel.
        /// </summary>
        /// <param name="cmsSettingsType">The CMSSettingsmodel </param>
        List<PropertyInfo> GetPropertiesOfType(Type cmsSettingsType);
        
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
        KeyValuePair<Type, Dictionary<CmsAttributes.CmsTabName, Dictionary<CmsAttributes.CmsGroupName, Dictionary<PropertyInfo, CmsPropertyAttribute>>>> GetAllPropertyAttributes(Type component);
        
        /// <summary>
        /// Retrieve the settingsmodel with data from the datalayer. This method will couple the data to the corresponding properties.
        /// </summary>
        /// <param name="component">The component to retrieve the properties of.</param>
        /// <returns>
        /// Dictionary with propertyinfo and the value of that property from the data.
        /// </returns>
        Task<Dictionary<PropertyInfo, object>> GetCmsSettingsModel(Type component, int templateId);

        /// <summary>
        /// Matches the component using reflection to retrieve its modes and saves the settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the content to save</param>
        /// <param name="component">A string of the component to match using reflection</param>
        /// <param name="componentMode">An int of the componentMode to match when the modes are retrieved</param>
        /// <param name="title">The name of the template to save</param>
        /// <param name="settings">A dictionary of settings containing their name and value</param>
        /// <returns>An int as confirmation of the affected rows</returns>
        Task<int> SaveNewSettings(ClaimsIdentity identity, int contentId, string component, int componentMode, string title, Dictionary<string, object> settings);
        
        /// <summary>
        /// Retrieve the component and componentMode of dynamic content with the given id.
        /// </summary>
        /// <param name="contentId">The id of the dynamic content</param>
        /// <returns>A list of strings containing the componentName and Mode.</returns>
        Task<List<string>> GetComponentAndModeForContentId(int contentId);
    }
}