using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Files.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.Files.Interfaces
{
    // TODO: Add documentation.
    public interface IFilesService
    {
        Task<ServiceResult<List<FileModel>>> UploadAsync(string encryptedId, string propertyName, string title, IFormFileCollection files, ClaimsIdentity identity, ulong itemLinkId = 0, bool useTinyPng = false);

        Task<ServiceResult<FileModel>> SaveFileInDatabaseAsync(ClaimsIdentity identity, byte[] fileBytes, string contentType, string fileName, string propertyName, string title = "", List<FtpSettingsModel> ftpSettings = null, string ftpDirectory = null, ulong itemId = 0, ulong itemLinkId = 0);

        Task<ServiceResult<(string ContentType, byte[] Data, string Url)>> GetFileAsync(string itemId, int fileId, ClaimsIdentity identity, ulong itemLinkId);

        Task<ServiceResult<bool>> DeleteFileAsync(string encryptedItemId, int fileId, ClaimsIdentity identity, ulong itemLinkId = 0);
        
        Task<ServiceResult<bool>> RenameFileAsync(string encryptedItemId, int fileId, string newName, ClaimsIdentity identity, ulong itemLinkId = 0);

        Task<ServiceResult<FileModel>> AddFileUrl(string encryptedItemId, string propertyName, FileModel file, ClaimsIdentity identity, ulong itemLinkId);
    }
}
