using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for doing things in database for Wiser templates.
    /// </summary>
    public interface ITemplateDataService
    {
        /// <summary>
        /// Get the meta data (name, changedOn, changedBy etc) from a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        Task<TemplateSettingsModel> GetMetaDataAsync(int templateId);

        /// <summary>
        /// Get the template data of a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <param name="environment">Optional: The environment the template needs to be active on. Get the latest version if no environment has been given.</param>
        /// <param name="version">Optional: If you want to get a specific version, enter that version number here.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        Task<TemplateSettingsModel> GetDataAsync(int templateId, Environments? environment = null, int? version = null);

        /// <summary>
        /// Get published environments from a template.
        /// </summary>
        /// <param name="templateId">The id of the template which environment should be retrieved.</param>
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>A list of all version and their published environment.</returns>
        Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int templateId, string branchDatabaseName = null);

        /// <summary>
        /// Get the ID, version number and published environment of the latest version of a template.
        /// </summary>
        /// <param name="templateId">The template ID.</param>
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>The ID, version number, published environment of the template and if it is removed.</returns>
        Task<(int Id, int Version, Environments Environment, bool Removed)> GetLatestVersionAsync(int templateId, string branchDatabaseName = null);

        /// <summary>
        /// Publish the template to an environment. This method will execute the publish model instructions it receives, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="templateId">The id of the template of which the environment should be published.</param>
        /// <param name="version">The version that should be deployed/published.</param>
        /// <param name="environment">The environment to publish the version of the template to.</param>
        /// <param name="publishLog">Information for the history of the template, to log the version change there.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        Task<int> UpdatePublishedEnvironmentAsync(int templateId, int version, Environments environment, PublishLogModel publishLog, string username, string branchDatabaseName = null);

        /// <summary>
        /// Get the templates linked to the current template and their relation to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template which linked templates should be retrieved.</param>
        /// <returns>Return a list of linked templates in the form of linkedtemplatemodels.</returns>
        Task<List<LinkedTemplateModel>> GetLinkedTemplatesAsync(int templateId);

        /// <summary>
        /// Get templates that can be linked to the current template but aren't linked yet.
        /// </summary>
        /// <param name="templateId">The id of the template for which the linkoptions should be retrieved.</param>
        /// <returns>A list of possible links in the form of linkedtemplatemodels.</returns>
        Task<List<LinkedTemplateModel>> GetTemplatesAvailableForLinkingAsync(int templateId);

        /// <summary>
        /// Get dynamic content that is linked to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content is to be retrieved.</param>
        /// <returns>A list of dynamic content data for all the dynamic content linked to the current template.</returns>
        Task<List<LinkedDynamicContentDao>> GetLinkedDynamicContentAsync(int templateId);

        /// <summary>
        /// Updates the latest version of a template with new data. This method will overwrite this version, unless this version has been published to the live environment,
        /// then it will create a new version as to not overwrite the live version.
        /// </summary>
        /// <param name="templateSettings">A <see cref="TemplateSettingsModel"/> containing the new data to save as a new template version.</param>
        /// <param name="templateLinks">A comma separated list of all linked javascript/scss/css templates.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        Task SaveAsync(TemplateSettingsModel templateSettings, string templateLinks, string username);

        /// <summary>
        /// Creates a new version of a template by copying the previous version and increasing the version number by one.
        /// </summary>
        /// <param name="templateId">The ID of the template to create a new version for.</param>
        /// <returns>The ID of the new version.</returns>
        Task<int> CreateNewVersionAsync(int templateId);

        /// <summary>
        /// Retrieves a section of the tree view around the given id. In case the id is 0 the root section of the tree will be retrieved.
        /// </summary>
        /// <param name="parentId">The id of the parent element of the tree section that needs to be retrieved</param>
        /// <returns>A list of <see cref="TemplateTreeViewDao"/> items that are children of the given id.</returns>
        Task<List<TemplateTreeViewDao>> GetTreeViewSectionAsync(int parentId);

        /// <summary>
        /// Searches for a template.
        /// </summary>
        /// <param name="searchValue">What to search for.</param>
        /// <param name="encryptionKey">The key used for encryption.</param>
        /// <returns></returns>
        Task<List<SearchResultModel>> SearchAsync(string searchValue, string encryptionKey);

        /// <summary>
        /// Creates an empty template with the given name, type and parent template.
        /// </summary>
        /// <param name="name">The name to give the template that will be created.</param>
        /// <param name="parent">The id of the parent template of the template that will be created. Enter null to add something to the root.</param>
        /// <param name="type">The type of the new template that will be created.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <param name="editorValue">The value to be inserted into the editor. This will be empty for blank templates.</param>
        /// <param name="ordering">Optional: The order number of the template. Leave empty to add it to the bottom. This will not update the ordering of any other templates, so make sure the order number doesn't exist yet if you enter one.</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        Task<int> CreateAsync(string name, int? parent, TemplateTypes type, string username, string editorValue = null, int? ordering  = null);

        /// <summary>
        /// Makes sure that the ordering of a tree view is correct, to prevent issues with drag and drop in the tree view.
        /// </summary>
        /// <param name="parentId">The ID of the parent in which to fix the ordering of all it's children.</param>
        Task FixTreeViewOrderingAsync(int parentId);

        /// <summary>
        /// Gets the parent ID of an item.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>The ID and name of the parent, or <see langword="null"/> if there is no parent.</returns>
        Task<TemplateSettingsModel> GetParentAsync(int templateId);

        /// <summary>
        /// Gets the sort order number of a template.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>The order number.</returns>
        Task<int> GetOrderingAsync(int templateId);

        /// <summary>
        /// Gets the highest order number of all children of a parent, so that you can calculate a new order number when adding a new child.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>The current highest order number.</returns>
        Task<int> GetHighestOrderNumberOfChildrenAsync(int? templateId);

        /// <summary>
        /// Moves a template to a new position.
        /// </summary>
        /// <param name="sourceId">The ID of the template that is being moved.</param>
        /// <param name="destinationId">The ID of the template or directory where it's being moved to.</param>
        /// <param name="sourceParentId">The original parent ID of the source item.</param>
        /// <param name="destinationParentId">The parent ID of the destination item.</param>
        /// <param name="oldOrderNumber">The original order number of the item that is being moved.</param>
        /// <param name="newOrderNumber">The new order number of the item that is being moved.</param>
        /// <param name="dropPosition">The drop position, can be either <see cref="TreeViewDropPositions.Over"/>, <see cref="TreeViewDropPositions.Before"/> or <see cref="TreeViewDropPositions.After"/>.</param>
        /// <param name="username">The name of the authenticated user.</param>
        Task MoveAsync(int sourceId, int destinationId, int sourceParentId, int destinationParentId, int oldOrderNumber, int newOrderNumber, TreeViewDropPositions dropPosition, string username);

        /// <summary>
        /// Get all extra SCSS that should be included with a SCSS template that needs to be compiled.
        /// </summary>
        /// <param name="templateId">The ID of the SCSS template to get the includes/imports for.</param>
        /// <returns>A <see cref="StringBuilder"/> with the contents of all SCSS in the correct order.</returns>
        Task<StringBuilder> GetScssIncludesForScssTemplateAsync(int templateId);

        /// <summary>
        /// Get all SCSS templates that are not include templates. These templates need to be recompiled when someone changes an include template.
        /// </summary>
        /// <param name="templateId">The ID of the SCSS template to get the includes/imports for.</param>
        /// <returns>A <see cref="StringBuilder"/> with the contents of all SCSS in the correct order.</returns>
        Task<List<TemplateSettingsModel>> GetScssTemplatesThatAreNotIncludesAsync(int templateId);

        /// <summary>
        /// Deletes a template.
        /// </summary>
        /// <param name="templateId">The ID of the template to delete.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <param name="alsoDeleteChildren">Optional: Whether or not to also delete all children of this template. Default value is <see langword="true"/>.</param>
        Task<bool> DeleteAsync(int templateId, string username, bool alsoDeleteChildren = true);

        /// <summary>
        /// Check if editor value is xml and if so, decrypt it.
        /// </summary>
        /// <param name="encryptionKey">The key used for encryption.</param>
        /// <param name="rawTemplateModel">The <see cref="TemplateSettingsModel"/> to perform the decryption on.</param>
        void DecryptEditorValueIfEncrypted(string encryptionKey, TemplateSettingsModel rawTemplateModel);

        /// <summary>
        /// Deploy one or more templates from the main branch to a sub branch.
        /// </summary>
        /// <param name="templateIds">The IDs of the templates to deploy.</param>
        /// <param name="branchDatabaseName">The name of the database that contains the sub branch.</param>
        Task DeployToBranchAsync(List<int> templateIds, string branchDatabaseName);

        /// <summary>
        /// Function that makes sure that the database tables needed for templates are up to date with the latest changes.
        /// </summary>
        Task KeepTablesUpToDateAsync();
    }
}