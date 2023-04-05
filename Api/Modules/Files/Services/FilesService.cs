using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Enums;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Files.Interfaces;
using Api.Modules.Files.Models;
using Api.Modules.CloudFlare.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Services;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Files.Services
{
    /// <inheritdoc cref="IFilesService" />
    public class FilesService : IFilesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly ILogger<FilesService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ICloudFlareService cloudFlareService;

        /// <summary>
        /// Creates a new instance of <see cref="FilesService"/>.
        /// </summary>
        public FilesService(IWiserCustomersService wiserCustomersService, ILogger<FilesService> logger, IDatabaseConnection databaseConnection, IWiserItemsService wiserItemsService, ICloudFlareService cloudFlareService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.cloudFlareService = cloudFlareService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<FileModel>>> UploadAsync(string encryptedId, string propertyName, string title, IFormFileCollection files, ClaimsIdentity identity, ulong itemLinkId = 0, bool useTinyPng = false, bool useCloudFlare = false, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedId))
            {
                throw new ArgumentNullException(nameof(encryptedId));
            }

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedId, identity);
            if (itemId <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.");
            }

            if (files == null || files.Count == 0)
            {
                return new ServiceResult<List<FileModel>>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No files found in the request"
                };
            }

            try
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
                if (!success)
                {
                    return new ServiceResult<List<FileModel>>
                    {
                        ErrorMessage = errorMessage,
                        StatusCode = HttpStatusCode.Forbidden
                    };
                }

                databaseConnection.ClearParameters();

                var (ftpDirectory, ftpSettings) = await GetFtpSettingsAsync(identity, itemLinkId, propertyName, itemId);

                if (useTinyPng)
                {
                    throw new NotImplementedException("Tiny PNG not supported yet.");
                }

                // Fix ordering of files.
                await FixOrderingAsync(itemId, itemLinkId, propertyName, identity);

                var result = new List<FileModel>();

                foreach (var file in files)
                {
                    byte[] fileBytes;
                    await using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    var fileName = file.FileName?.Trim('"');
                    fileName = Path.GetFileNameWithoutExtension(fileName).ConvertToSeo() + Path.GetExtension(fileName)?.ToLowerInvariant();
                    var fileExtension = Path.GetExtension(fileName);

                    if (useTinyPng && (fileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase) || fileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new NotImplementedException("Tiny PNG not supported yet.");
                    }

                    var fileResult = await SaveAsync(identity, fileBytes, file.ContentType, fileName, propertyName, title, ftpSettings, ftpDirectory, itemId, itemLinkId, useCloudFlare, entityType, linkType);
                    if (fileResult.StatusCode != HttpStatusCode.OK)
                    {
                        return new ServiceResult<List<FileModel>>
                        {
                            StatusCode = fileResult.StatusCode,
                            ErrorMessage = fileResult.ErrorMessage
                        };
                    }

                    result.Add(fileResult.ModelObject);
                }

                return new ServiceResult<List<FileModel>>(result);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == 1153)
                {
                    return new ServiceResult<List<FileModel>>
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessage = "File is to large for database."
                    };
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<FileModel>> SaveAsync(ClaimsIdentity identity, byte[] fileBytes, string contentType, string fileName, string propertyName, string title = "", List<FtpSettingsModel> ftpSettings = null, string ftpDirectory = null, ulong itemId = 0, ulong itemLinkId = 0, bool useCloudFlare = false, string entityType = null, int linkType = 0)
        {
            var content = Array.Empty<byte>();
            var contentUrl = string.Empty;
            var fileExtension = Path.GetExtension(fileName);
            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            if ((ftpSettings == null || !ftpSettings.Any()) && !useCloudFlare)
            {
                content = fileBytes;
            }
            else if (useCloudFlare)
            {
                contentUrl = await cloudFlareService.UploadImageAsync(fileName, fileBytes);
            }
            else
            {
                // Add GUID to file name to make sure we always have a unique name, otherwise you can never upload multiple files in a single file-upload field.
                var localFileName = $"{itemId}_{propertyName}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
                var ftpPath = Path.Combine("/", (ftpSettings.First().RootDirectory ?? ""), ftpDirectory ?? "");
                var ftpFileLocation = Path.Combine(ftpPath, localFileName).Replace(@"\", @"/");
                contentUrl = ftpFileLocation;

                var succeededFtpUploads = new List<FtpSettingsModel>();
                try
                {
                    foreach (var ftp in ftpSettings)
                    {
                        if (ftp.Mode == FtpModes.Sftp)
                        {
                            throw new NotImplementedException("SFTP is not yet supported");
                            /*using (var client = new SftpClient(ftp.Host, ftp.Username, ftp.Password))
                            {
                                await using var fileStream = new MemoryStream(fileBytes);
                                client.Connect();
                                client.UploadFile(fileStream, ftpFileLocation);
                            }*/
                        }
                        else
                        {
                            // Get the object used to communicate with the server.
                            var fullFtpLocation = $"ftp://{ftp.Host}{ftpFileLocation}";
                            var request = (FtpWebRequest)WebRequest.Create(fullFtpLocation);
                            request.Method = WebRequestMethods.Ftp.UploadFile;
                            request.Credentials = new NetworkCredential(ftp.Username, ftp.Password);
                            // Copy the contents of the file to the request stream.
                            request.ContentLength = fileBytes.Length;

                            await using var requestStream = request.GetRequestStream();
                            await requestStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }

                        succeededFtpUploads.Add(ftp);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError($"Error while trying to upload file via FTP: {exception}");

                    var errorMessage = $"Er is iets fout gegaan tijdens het uploaden van het bestand via FTP. Probeer het aub opnieuw of neem contact op met ons.<br><br>De fout was:<br>{exception.Message}";
                    if (!succeededFtpUploads.Any())
                    {
                        return new ServiceResult<FileModel>
                        {
                            StatusCode = HttpStatusCode.InternalServerError,
                            ErrorMessage = errorMessage
                        };
                    }

                    try
                    {
                        foreach (var ftp in succeededFtpUploads)
                        {
                            await DeleteFileFromFtp(ftp, ftpFileLocation);
                        }
                    }
                    catch (Exception deleteException)
                    {
                        logger.LogError($"We got an error while uploading to one of the FTP servers, after at least one already succeeded. So we tried to delete the file from the FTP that succeeded, but that also failed with the exception: {deleteException}");
                    }

                    errorMessage = $"Er is iets fout gegaan tijdens het uploaden van het bestand via FTP.<br>Op {succeededFtpUploads.Count} van de {ftpSettings.Count} servers is het wel gelukt, maar bij de eerstvolgende niet.<br>Het uploaden is daarom ongedaan gemaakt op alle servers.<br>Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br>{exception.Message}";
                    return new ServiceResult<FileModel>
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        ErrorMessage = errorMessage
                    };
                }
            }

            var username = IdentityHelpers.GetUserName(identity, true);
            databaseConnection.ClearParameters();
            var columnsForInsertQuery = new List<string> { "content_url", "content_type", "file_name", "extension", "added_by", "title", "property_name", "ordering" };
            var tablePrefix = "";
            if (itemLinkId > 0)
            {
                tablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType, entityType);
                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("itemlink_id", itemLinkId);
                columnsForInsertQuery.Add("itemlink_id");
            }
            else if (itemId > 0)
            {
                tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("item_id", itemId);
                columnsForInsertQuery.Add("item_id");
            }
            else
            {
                databaseConnection.AddParameter("itemlink_id", 0);
                databaseConnection.AddParameter("item_id", 0);
            }

            if (content?.Length > 0)
            {
                databaseConnection.AddParameter("content", content);
                columnsForInsertQuery.Add("content");
            }

            databaseConnection.AddParameter("content_url", contentUrl);
            databaseConnection.AddParameter("content_type", contentType);
            databaseConnection.AddParameter("file_name", fileName);
            databaseConnection.AddParameter("extension", Path.GetExtension(fileName));
            databaseConnection.AddParameter("added_by", username ?? "");
            databaseConnection.AddParameter("title", title ?? "");
            databaseConnection.AddParameter("property_name", propertyName);

            var ordering = 1;
            var whereClause = itemLinkId > 0 ? "itemlink_id = ?itemlink_id" : "item_id = ?item_id";
            var query = $@"SELECT IFNULL(MAX(ordering), 0) AS maxOrdering
FROM {tablePrefix}{WiserTableNames.WiserItemFile}
WHERE {whereClause}
AND property_name = ?property_name";
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count > 0)
            {
                ordering = Convert.ToInt32(dataTable.Rows[0]["maxOrdering"]) + 1;
            }
            databaseConnection.AddParameter("ordering", ordering);

            query = $@"SET @_username = ?added_by;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItemFile} ({String.Join(", ", columnsForInsertQuery)})
VALUES ({String.Join(", ", columnsForInsertQuery.Select(x => $"?{x}"))});
SELECT LAST_INSERT_ID() AS newId;";
            var newItem = await databaseConnection.GetAsync(query);

            var result = new FileModel
            {
                FileId = Convert.ToInt32(newItem.Rows[0]["newId"]),
                Name = fileName,
                Extension = fileExtension,
                ItemId = await wiserCustomersService.EncryptValue(itemId, identity),
                Size = fileBytes.Length,
                Title = title,
                ContentType = contentType,
                EntityType = entityType,
                LinkType = linkType
            };

            return new ServiceResult<FileModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<(string ContentType, byte[] Data, string Url)>> GetAsync(string encryptedItemId, int fileId, ClaimsIdentity identity, ulong itemLinkId, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            if (!UInt64.TryParse(encryptedItemId, out var itemId))
            {
                itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            }

            if (fileId <= 0)
            {
                throw new ArgumentException("Image ID must be greater than zero.");
            }

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var query = $"SELECT content_type, content, content_url, file_name, property_name FROM {tablePrefix}{WiserTableNames.WiserItemFile} WHERE id = ?imageId";

            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("imageId", fileId);
            var dataTable = await databaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<(string ContentType, byte[] Data, string Url)>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "File not found"
                };
            }

            var contentUrl = dataTable.Rows[0].Field<string>("content_url");
            var contentType = dataTable.Rows[0].Field<string>("content_type");
            var propertyName = dataTable.Rows[0].Field<string>("property_name");
            byte[] data = null;
            if (!dataTable.Rows[0].IsNull("content"))
            {
                data = dataTable.Rows[0].Field<byte[]>("content");
                return new ServiceResult<(string ContentType, byte[] Data, string url)>((ContentType: contentType, Data: data, url: null));
            }

            if (String.IsNullOrWhiteSpace(contentUrl))
            {
                return new ServiceResult<(string ContentType, byte[] Data, string Url)>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "File not found"
                };
            }

            var (_, ftpSettings) = await GetFtpSettingsAsync(identity, itemLinkId, propertyName, itemId);
            if (!ftpSettings.Any())
            {
                return new ServiceResult<(string ContentType, byte[] Data, string Url)>((ContentType: null, Data: null, Url: contentUrl));
            }

            var ftp = ftpSettings.First();
            if (ftp.Mode == FtpModes.Sftp)
            {
                throw new NotImplementedException("SFTP is not yet supported");
                /*using var client = new SftpClient(ftp.Host, ftp.Username, ftp.Password);
                await using var memoryStream = new MemoryStream();
                client.Connect();
                client.DownloadFile(contentUrl, memoryStream);
                return new ServiceResult<(string ContentType, byte[] Data, string url)>((ContentType: contentType, Data: memoryStream.ToArray(), url: null));*/
            }

            // Get the object used to communicate with the server.
            var fullFtpLocation = $"ftp://{ftp.Host}{contentUrl}";
            var request = (FtpWebRequest)WebRequest.Create(fullFtpLocation);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(ftp.Username, ftp.Password);

            using var response = await request.GetResponseAsync();
            await using var responseStream = response.GetResponseStream();
            if (responseStream == null)
            {
                return new ServiceResult<(string ContentType, byte[] Data, string Url)>((ContentType: null, Data: null, Url: null));
            }

            await using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);
            return new ServiceResult<(string ContentType, byte[] Data, string Url)>((ContentType: contentType, Data: memoryStream.ToArray(), Url: null));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(string encryptedItemId, int fileId, ClaimsIdentity identity, ulong itemLinkId = 0, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            if (itemId <= 0 && itemLinkId <= 0)
            {
                throw new ArgumentException("Id or itemLinkId must be greater than zero.");
            }

            if (fileId <= 0)
            {
                throw new ArgumentException("File ID must be greater than zero.");
            }

            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
            if (!success)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", itemLinkId > 0 ? itemLinkId : itemId);
            databaseConnection.AddParameter("fileId", fileId);
            var query = $"SELECT property_name FROM {tablePrefix}{WiserTableNames.WiserItemFile} WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<bool>(false) { StatusCode = HttpStatusCode.NotFound };
            }

            var propertyName = dataTable.Rows[0].Field<string>("property_name");

            // Delete the files from the FTP(s), if this file-input uses FTP upload
            // else delete them from CloudFlare if url matches
            query = $"SELECT content_url FROM {WiserTableNames.WiserItemFile} WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";
            dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<bool>(true) { StatusCode = HttpStatusCode.NoContent };
            }

            var fileUrl = dataTable.Rows[0].Field<string>("content_url");
            var (_, ftpSettings) = await GetFtpSettingsAsync(identity, itemLinkId, propertyName, itemId);
            if (ftpSettings.Any())
            {
                if (!String.IsNullOrWhiteSpace(fileUrl))
                {
                    foreach (var ftp in ftpSettings)
                    {
                        await DeleteFileFromFtp(ftp, fileUrl);
                    }
                }
            }
            else if(!String.IsNullOrWhiteSpace(fileUrl))
            {
                // CLoudFlare file?
                if (fileUrl.StartsWith("https://imagedelivery.net"))
                {
                    await cloudFlareService.DeleteImageAsync(fileUrl);
                }
            }

            query = $"DELETE FROM {tablePrefix}{WiserTableNames.WiserItemFile} WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";
            await databaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true) { StatusCode = HttpStatusCode.NoContent };
        }

        private static async Task DeleteFileFromFtp(FtpSettingsModel ftp, string fileUrl)
        {
            if (ftp.Mode == FtpModes.Sftp)
            {
                throw new NotImplementedException("SFTP is not yet supported");
                /*using var client = new SftpClient(ftp.Host, ftp.Username, ftp.Password);
                client.Connect();
                client.DeleteFile(fileUrl);*/
            }
            else
            {
                // Get the object used to communicate with the server.
                var fullFtpLocation = $"ftp://{ftp.Host}{fileUrl}";
                var request = (FtpWebRequest)WebRequest.Create(fullFtpLocation);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftp.Username, ftp.Password);
                using var response = await request.GetResponseAsync();
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> RenameAsync(string encryptedItemId, int fileId, string newName, ClaimsIdentity identity, ulong itemLinkId = 0, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            if (itemId <= 0 && itemLinkId <= 0)
            {
                throw new ArgumentException("Id or itemLinkId must be greater than zero.");
            }

            if (fileId <= 0)
            {
                throw new ArgumentException("File ID must be greater than zero.");
            }

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var query = $"UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} SET file_name = ?newName WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";

            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
            if (!success)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", itemLinkId > 0 ? itemLinkId : itemId);
            databaseConnection.AddParameter("fileId", fileId);
            databaseConnection.AddParameter("newName", Path.GetFileNameWithoutExtension(newName).ConvertToSeo() + Path.GetExtension(newName).ToLowerInvariant());
            await databaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true) { StatusCode = HttpStatusCode.NoContent };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateTitleAsync(string encryptedItemId, int fileId, string newTitle, ClaimsIdentity identity, ulong itemLinkId = 0, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            if (itemId <= 0 && itemLinkId <= 0)
            {
                throw new ArgumentException("Id or itemLinkId must be greater than zero.");
            }

            if (fileId <= 0)
            {
                throw new ArgumentException("File ID must be greater than zero.");
            }

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var query = $"UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} SET title = ?newTitle WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";

            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
            if (!success)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", itemLinkId > 0 ? itemLinkId : itemId);
            databaseConnection.AddParameter("fileId", fileId);
            databaseConnection.AddParameter("newTitle", newTitle);
            await databaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true) { StatusCode = HttpStatusCode.NoContent };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateExtraDataAsync(string encryptedItemId, int fileId, FileExtraDataModel extraData, ClaimsIdentity identity, ulong itemLinkId = 0, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            if (itemId <= 0 && itemLinkId <= 0)
            {
                throw new ArgumentException("Id or itemLinkId must be greater than zero.");
            }

            if (fileId <= 0)
            {
                throw new ArgumentException("File ID must be greater than zero.");
            }

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var query = $@"UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} SET extra_data = ?extraData WHERE item{(itemLinkId > 0 ? "link" : "")}_id = ?id AND id = ?fileId";

            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
            if (!success)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", itemLinkId > 0 ? itemLinkId : itemId);
            databaseConnection.AddParameter("fileId", fileId);
            databaseConnection.AddParameter("extraData", extraData == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(extraData));
            await databaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true) { StatusCode = HttpStatusCode.NoContent };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<FileModel>> AddUrlAsync(string encryptedItemId, string propertyName, FileModel file, ClaimsIdentity identity, ulong itemLinkId, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(encryptedItemId))
            {
                throw new ArgumentNullException(nameof(encryptedItemId));
            }

            var itemId = await wiserCustomersService.DecryptValue<ulong>(encryptedItemId, identity);
            if (itemId <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.");
            }

            if (file == null || String.IsNullOrWhiteSpace(file.ContentUrl))
            {
                return new ServiceResult<FileModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No files found in the request"
                };
            }

            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
            if (!success)
            {
                return new ServiceResult<FileModel>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            databaseConnection.ClearParameters();
            var columnsForInsertQuery = new List<string> { "content_url", "content_type", "file_name", "extension", "added_by", "title", "property_name", "ordering" };

            if (itemLinkId > 0)
            {
                databaseConnection.AddParameter("itemlink_id", itemLinkId);
                columnsForInsertQuery.Add("itemlink_id");
            }
            else
            {
                databaseConnection.AddParameter("item_id", itemId);
                columnsForInsertQuery.Add("item_id");
            }

            file.Name = String.IsNullOrWhiteSpace(file.Name) ? Path.GetFileName(file.ContentUrl) : file.Name;
            file.Extension = String.IsNullOrWhiteSpace(file.Extension) ? Path.GetExtension(file.Name) : file.Extension;

            databaseConnection.AddParameter("content_type", file.ContentType ?? "");
            databaseConnection.AddParameter("content_url", file.ContentUrl);
            databaseConnection.AddParameter("file_name", Path.GetFileNameWithoutExtension(file.Name).ConvertToSeo() + Path.GetExtension(file.Name)?.ToLowerInvariant());
            databaseConnection.AddParameter("extension", file.Extension);
            databaseConnection.AddParameter("added_by", IdentityHelpers.GetUserName(identity, true) ?? "");
            databaseConnection.AddParameter("title", file.Title ?? "");
            databaseConnection.AddParameter("property_name", propertyName);

            var query = $@"SET @_username = ?added_by;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItemFile} ({String.Join(", ", columnsForInsertQuery)})
VALUES ({String.Join(", ", columnsForInsertQuery.Select(x => $"?{x}"))});
SELECT LAST_INSERT_ID() AS newId;";
            var newItem = await databaseConnection.GetAsync(query);
            file.FileId = Convert.ToInt32(newItem.Rows[0]["newId"]);

            return new ServiceResult<FileModel>(file);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateOrderingAsync(ClaimsIdentity identity, int fileId, int previousPosition, int newPosition, ulong itemId, string propertyName, ulong itemLinkId = 0, string entityType = null, int linkType = 0)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();

            var userId = IdentityHelpers.GetWiserUserId(identity);
            var (success, errorMessage, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId);
            if (!success)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = errorMessage,
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            if (newPosition == previousPosition)
            {
                // Don't need to do anything if the position isn't changed.
                return new ServiceResult<bool>(true);
            }

            databaseConnection.AddParameter("fileId", fileId);
            databaseConnection.AddParameter("previousPosition", previousPosition);
            databaseConnection.AddParameter("newPosition", newPosition);
            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("propertyName", propertyName);
            databaseConnection.AddParameter("itemLinkId", itemLinkId);
            databaseConnection.AddParameter("username", IdentityHelpers.GetUserName(identity, true));

            var whereClause = itemLinkId > 0 ? "itemlink_id = ?itemLinkId" : "item_id = ?itemId";

            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            string query;
            if (newPosition < previousPosition)
            {
                // Increase the ordering of all files that come later than the new position and earlier than the previous position.
                query = $@"SET @_username = ?username;
UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} 
SET ordering = ordering + 1
WHERE {whereClause}
AND property_name = ?propertyName
AND ordering >= ?newPosition
AND ordering < ?previousPosition";
                await databaseConnection.ExecuteAsync(query);
            }
            else
            {
                // Lower the ordering of all files that come later than the previous position and earlier than the new position.
                query = $@"SET @_username = ?username;
UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} 
SET ordering = ordering - 1
WHERE {whereClause}
AND property_name = ?propertyName
AND ordering > ?previousPosition
AND ordering <= ?newPosition";
                await databaseConnection.ExecuteAsync(query);
            }

            // Move the file to the new position.
            query = $@"UPDATE {WiserTableNames.WiserItemFile} SET ordering = ?newPosition WHERE id = ?fileId";
            await databaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task FixOrderingAsync(ulong itemId, ulong itemLinkId, string propertyName, ClaimsIdentity identity, string entityType = null, int linkType = 0)
        {
            var tablePrefix = itemLinkId > 0
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType)
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("itemLinkId", itemLinkId);
            databaseConnection.AddParameter("propertyName", propertyName);
            databaseConnection.AddParameter("username", IdentityHelpers.GetUserName(identity, true));
            var whereClause = itemLinkId > 0 ? "itemlink_id = ?itemLinkId" : "item_id = ?itemId";
            var query = $@"SET @orderingNumber = 0;
SET @_username = ?username;

UPDATE {tablePrefix}{WiserTableNames.WiserItemFile} AS file
JOIN (
	SELECT 
		x.id,
		(@orderingNumber := @orderingNumber + 1) AS ordering
	FROM (
		SELECT
			id
		FROM {tablePrefix}{WiserTableNames.WiserItemFile}
		WHERE {whereClause}
		AND property_name = ?propertyName
		ORDER BY ordering ASC, id ASC
	) AS x
) AS ordering ON ordering.id = file.id
SET file.ordering = ordering.ordering
WHERE {whereClause}
AND property_name = ?propertyName";
            await databaseConnection.ExecuteAsync(query);
        }

        private async Task<(string UploadDirectory, List<FtpSettingsModel> FtpSettings)> GetFtpSettingsAsync(ClaimsIdentity identity, ulong itemLinkId, string propertyName, ulong itemId)
        {
            var ftpSettings = new List<FtpSettingsModel>();
            string query;

            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            databaseConnection.AddParameter("propertyName", propertyName);

            if (itemLinkId > 0)
            {
                databaseConnection.AddParameter("itemLinkId", itemLinkId);
                query = $@"SELECT options FROM {WiserTableNames.WiserEntityProperty} WHERE link_type = ?itemLinkId AND property_name = ?propertyName LIMIT 1";
            }
            else
            {
                databaseConnection.AddParameter("itemId", itemId);
                query = $@"SELECT p.options 
                            FROM {WiserTableNames.WiserEntityProperty} AS p 
                            JOIN wiser_item AS i ON i.id = ?itemId AND i.entity_type = p.entity_name
                            WHERE property_name = ?propertyName
                            LIMIT 1";
            }

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return (null, ftpSettings);
            }

            var options = dataTable.Rows[0].Field<string>("options");
            if (String.IsNullOrWhiteSpace(options))
            {
                return (null, ftpSettings);
            }

            var parsedOptions = JObject.Parse(options);
            if (!parsedOptions.Value<bool>("useFtp"))
            {
                return (null, ftpSettings);
            }

            var ftpDirectory = parsedOptions.Value<string>("uploadDirectory") ?? "";

            query = $@"SELECT
                        host.value AS host,
                        mode.value AS mode,
                        username.value AS username,
                        password.value AS password,
                        root_directory.value AS root_directory
                    FROM {WiserTableNames.WiserItem} AS item
                    JOIN {WiserTableNames.WiserItemDetail} AS host ON host.item_id = item.id AND host.`key` = 'host' AND host.value <> ''
                    JOIN {WiserTableNames.WiserItemDetail} AS mode ON mode.item_id = item.id AND mode.`key` = 'mode' AND mode.value <> ''
                    JOIN {WiserTableNames.WiserItemDetail} AS username ON username.item_id = item.id AND username.`key` = 'username' AND username.value <> ''
                    JOIN {WiserTableNames.WiserItemDetail} AS password ON password.item_id = item.id AND password.`key` = 'password' AND password.value <> ''
                    LEFT JOIN {WiserTableNames.WiserItemDetail} AS root_directory ON root_directory.item_id = item.id AND root_directory.`key` = 'root_directory'
                    WHERE item.entity_type = 'ftp_configuration'";
            dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return (null, ftpSettings);
            }

            var customer = await wiserCustomersService.GetSingleAsync(identity);
            var encryptionKey = parsedOptions.Value<string>(WiserItemsService.SecurityKeyKey);
            if (String.IsNullOrWhiteSpace(encryptionKey))
            {
                encryptionKey = customer.ModelObject.EncryptionKey;
            }

            ftpSettings.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new FtpSettingsModel
            {
                Username = dataRow.Field<string>("username"),
                Host = dataRow.Field<string>("host"),
                Password = dataRow.Field<string>("password").DecryptWithAesWithSalt(encryptionKey),
                RootDirectory = dataRow.Field<string>("root_directory") ?? "",
                Mode = (FtpModes)Enum.Parse(typeof(FtpModes), dataRow.Field<string>("mode") ?? "Ftp", true)
            }));

            return (ftpDirectory, ftpSettings);
        }
    }
}