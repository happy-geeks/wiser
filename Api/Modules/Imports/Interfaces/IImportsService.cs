using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Imports.Models;

namespace Api.Modules.Imports.Interfaces
{
    //TODO Verify comments
    /// <summary>
    /// Service for the import module and the import/export module for delete by import.
    /// </summary>
    public interface IImportsService
    {
        /// <summary>
        /// Prepare an import to be imported by the AIS.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="importRequest">The information needed for the import.</param>
        /// <returns></returns>
        Task<ServiceResult<ImportResultModel>> PrepareImportAsync(ClaimsIdentity identity, ImportRequestModel importRequest);

        /// <summary>
        /// Prepare the items to delete by finding all item ids matching the criteria of the <see cref="DeleteItemsRequestModel"/>.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="deleteItemsRequest">The criteria for the items to delete.</param>
        /// <returns>Returns a <see cref="DeleteItemsConfirmModel"/> containing all the item ids to delete.</returns>
        Task<ServiceResult<DeleteItemsConfirmModel>> PrepareDeleteItems(ClaimsIdentity identity, DeleteItemsRequestModel deleteItemsRequest);

        /// <summary>
        /// Delete the items corresponding with the provided item ids.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="deleteItemsConfirm">The <see cref="DeleteItemsConfirmModel"/> containing the item ids to delete the items from.</param>
        /// <returns>Returns true on success.</returns>
        Task<ServiceResult<bool>> DeleteItems(ClaimsIdentity identity, DeleteItemsConfirmModel deleteItemsConfirm);


        /// <summary>
        /// Prepare the links to delete by finding all item link ids matching the criteria of the <see cref="DeleteLinksRequestModel"/>.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="deleteLinksRequest">The criteria for the item links to delete.</param>
        /// <returns>Returns a collection of <see cref="DeleteLinksConfirmModel"/> containing information to delete the links.</returns>
        Task<ServiceResult<List<DeleteLinksConfirmModel>>> PrepareDeleteLinks(ClaimsIdentity identity, DeleteLinksRequestModel deleteLinksRequest);

        /// <summary>
        /// Delete the links corresponding to the provided information.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="deleteLinksConfirms">A collection of <see cref="DeleteLinksConfirmModel"/>s containing the information about the links to delete.</param>
        /// <returns>Returns true on success.</returns>
        Task<ServiceResult<bool>> DeleteLinks(ClaimsIdentity identity, List<DeleteLinksConfirmModel> deleteLinksConfirms);
    }
}
