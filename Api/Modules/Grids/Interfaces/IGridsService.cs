using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Grids.Enums;
using Api.Modules.Grids.Models;
using Api.Modules.Kendo.Models;

namespace Api.Modules.Grids.Interfaces
{
    // TODO: Add documentation.
    /// <summary>
    /// A service for doing things with (Kendo) grids.
    /// </summary>
    public interface IGridsService
    {
        /// <summary>
        /// Get the data for a sub-entities-grid.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="entityType">The entity type of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="linkTypeNumber">The link type number to use for getting linked items.</param>
        /// <param name="moduleId">The module ID of the items to get.</param>
        /// <param name="mode">The mode that the sub-entities-grid is in.</param>
        /// <param name="options">The options for the grid.</param>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedQueryId">Optional: The encrypted ID of the query to execute for getting the data.</param>
        /// <param name="encryptedCountQueryId">Optional: The encrypted ID of the query to execute for counting the total amount of items.</param>
        /// <param name="fieldGroupName">Optional: The field group name, when getting all fields of a group.</param>
        /// <param name="currentItemIsSourceId">Optional: Whether the opened item (that contains the sub-entities-grid) is the source instead of the destination.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The data of the grid, as a <see cref="GridSettingsAndDataModel"/>.</returns>
        Task<ServiceResult<GridSettingsAndDataModel>> GetEntityGridDataAsync(string encryptedId, string entityType, int linkTypeNumber, int moduleId, EntityGridModes mode, GridReadOptionsModel options, int propertyId, string encryptedQueryId, string encryptedCountQueryId, string fieldGroupName, bool currentItemIsSourceId, ClaimsIdentity identity);

        /// <summary>
        /// Get the data and settings for a module with grid view mode enabled.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="options">The options for the Kendo UI grid.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="isForExport">Whether this data is going to be used to export it to Excel.</param>
        Task<ServiceResult<GridSettingsAndDataModel>> GetOverviewGridDataAsync(int moduleId, GridReadOptionsModel options, ClaimsIdentity identity, bool isForExport = false);

        /// <summary>
        /// Get the data for a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="options">The options for the grid.</param>
        /// <param name="encryptedQueryId">Optional: The encrypted ID of the query to execute for getting the data.</param>
        /// <param name="encryptedCountQueryId">Optional: The encrypted ID of the query to execute for counting the total amount of items.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The data of the grid, as a <see cref="GridSettingsAndDataModel"/>.</returns>
        Task<ServiceResult<GridSettingsAndDataModel>> GetDataAsync(int propertyId, string encryptedId, GridReadOptionsModel options, string encryptedQueryId, string encryptedCountQueryId, ClaimsIdentity identity);

        /// <summary>
        /// Insert a new row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The data for the new row.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The newly added data.</returns>
        Task<ServiceResult<Dictionary<string, object>>> InsertDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity);

        /// <summary>
        /// Update a row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The new data for the row.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<bool>> UpdateDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity);

        /// <summary>
        /// Delete a row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The new data for the row.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<bool>> DeleteDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity);

    }
}