using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Files.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.Files.Interfaces
{
    /// <summary>
    /// Service for handling files for Wiser items.
    /// </summary>
    public interface IFilesService
    {
        /// <summary>
        /// Upload one or more files for an item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item the file should be linked to.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="title">The title/description of the file.</param>
        /// <param name="files">The uploaded files.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="useTinyPng">Optional: Whether to use tiny PNG to compress image files, one or more image files are being uploaded.</param>
        /// <returns>A list of <see cref="FileModel"/> with file data.</returns>
        Task<ServiceResult<List<FileModel>>> UploadAsync(string encryptedId, string propertyName, string title, IFormFileCollection files, ClaimsIdentity identity, ulong itemLinkId = 0, bool useTinyPng = false);

        /// <summary>
        /// Save a file to the database or FTP. By default, the file will be saved in the database (wiser_itemfile), unless FTP settings are given.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="fileBytes">The file contents.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="title">The title/description of the file.</param>
        /// <param name="ftpSettings">Optional: If the file should be uploaded to an FTP server, enter those settings here.</param>
        /// <param name="ftpDirectory">Optional: If the file should be uploaded to an FTP server, enter the directory for that here.</param>
        /// <param name="itemId">Optional: The ID of the item the file should be linked to.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <returns>A list of <see cref="FileModel"/> with file data.</returns>
        Task<ServiceResult<FileModel>> SaveFileAsync(ClaimsIdentity identity, byte[] fileBytes, string contentType, string fileName, string propertyName, string title = "", List<FtpSettingsModel> ftpSettings = null, string ftpDirectory = null, ulong itemId = 0, ulong itemLinkId = 0);

        /// <summary>
        /// Gets a file of an item.
        /// </summary>
        /// <param name="itemId">The encrypted ID of the item to get the file of.</param>
        /// <param name="fileId">The ID of the file to get.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <returns>The content type, contents and URL of the file.</returns>
        Task<ServiceResult<(string ContentType, byte[] Data, string Url)>> GetFileAsync(string itemId, int fileId, ClaimsIdentity identity, ulong itemLinkId);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        Task<ServiceResult<bool>> DeleteFileAsync(string encryptedItemId, int fileId, ClaimsIdentity identity, ulong itemLinkId = 0);
        
        /// <summary>
        /// Change the name of a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="newName">The new name of the file.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        Task<ServiceResult<bool>> RenameFileAsync(string encryptedItemId, int fileId, string newName, ClaimsIdentity identity, ulong itemLinkId = 0);

        /// <summary>
        /// Change the title/description of a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="newTitle">The new title/description of the file.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        Task<ServiceResult<bool>> UpdateFileTitleAsync(string encryptedItemId, int fileId, string newTitle, ClaimsIdentity identity, ulong itemLinkId = 0);

        /// <summary>
        /// Adds an URL to an external file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="file">The file data.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <returns>The <see cref="FileModel">FileModel</see> of the new file.</returns>
        Task<ServiceResult<FileModel>> AddFileUrl(string encryptedItemId, string propertyName, FileModel file, ClaimsIdentity identity, ulong itemLinkId);
    }
}
