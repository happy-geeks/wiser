using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using Api.Modules.Files.Interfaces;
using Api.Modules.Files.Models;
using Api.Modules.Items.Controllers;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Modules.Files.Controllers
{
    /// <summary>
    /// Controller for all operations that have something to do with Wiser item files.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class FilesController : Controller
    {
        private readonly IFilesService filesService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="ItemsController"/>.
        /// </summary>
        public FilesController(IFilesService filesService, IOptions<GclSettings> gclSettings)
        {
            this.filesService = filesService;
            this.gclSettings = gclSettings.Value;
        }

        /// <summary>
        /// Gets all items in a tree view from a parent.
        /// </summary>
        /// <param name="parentId">The parent ID. Enter 0 to get items from the root directory..</param>
        /// <returns>A list of <see cref="FileTreeViewModel"/>.</returns>
        [HttpGet]
        [Route("{parentId:int}/tree")]
        [ProducesResponseType(typeof(List<FileTreeViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTreeAsync(ulong parentId)
        {
            return (await filesService.GetTreeAsync((ClaimsIdentity) User.Identity, parentId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Upload one or more files for an item.
        /// The files should be included in the request as multi part form data.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item the file should be linked to.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="title">The title/description of the file.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="useTinyPng">Optional: Whether to use tiny PNG to compress image files, one or more image files are being uploaded.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        /// <param name="useCloudFlare">Optional: Whether to use CloudFlare to store image files.</param>
        /// <returns>A list of <see cref="FileModel"/> with file data.</returns>
        [HttpPost]
        [Route("~/api/v3/items/{encryptedId}/upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAsync(string encryptedId, [FromQuery]string propertyName, [FromQuery]string title = "", [FromQuery]ulong itemLinkId = 0, [FromQuery]bool useTinyPng = false, [FromQuery] bool useCloudFlare = false, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            var form = await Request.ReadFormAsync();

            var result = await filesService.UploadAsync(encryptedId, propertyName, title, form.Files, (ClaimsIdentity)User.Identity, itemLinkId, useTinyPng, useCloudFlare, entityType, linkType);
            return result.GetHttpResponseMessage();
        }

        /// <summary>
        /// Adds an URL to an external file.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="file">The file data.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        /// <returns>The <see cref="FileModel">FileModel</see> of the new file.</returns>
        [HttpPost]
        [Route("~/api/v3/items/{encryptedId}/files/url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddFileUrlAsync(string encryptedId, [FromBody]FileModel file, [FromQuery]string propertyName, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            var result = await filesService.AddUrlAsync(encryptedId, propertyName, file, (ClaimsIdentity)User.Identity, itemLinkId, entityType, linkType);
            return result.GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets a file of an item.
        /// </summary>
        /// <param name="itemId">The encrypted ID of the item to get the file of.</param>
        /// <param name="fileId">The ID of the file to get.</param>
        /// <param name="fileName">The full file name to return (including extension).</param>
        /// <param name="customerInformation">Information about the authenticated user, such as the encrypted user ID.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        /// <returns>The file contents.</returns>
        [HttpGet]
        [Route("~/api/v3/items/{itemId}/files/{fileId:int}/{filename}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Produces(MediaTypeNames.Application.Octet)]
        public async Task<IActionResult> GetFileAsync(string itemId, int fileId, string fileName, [FromQuery] CustomerInformationModel customerInformation, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(customerInformation.encryptedUserId) ? 0 : Int32.Parse(customerInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.GroupSid, customerInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = customerInformation.subDomain;

            var imageResult = await filesService.GetAsync(itemId, fileId, dummyClaimsIdentity, itemLinkId, entityType, linkType);
            var result = imageResult.GetHttpResponseMessage();
            if (imageResult.StatusCode != HttpStatusCode.OK)
            {
                return result;
            }

            if (!String.IsNullOrWhiteSpace(imageResult.ModelObject.Url))
            {
                imageResult.StatusCode = HttpStatusCode.Found;
                return Redirect(imageResult.ModelObject.Url);
            }

            return File(imageResult.ModelObject.Data, imageResult.ModelObject.ContentType);
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        [HttpDelete]
        [Route("~/api/v3/items/{encryptedItemId}/files/{fileId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteFileAsync(string encryptedItemId, int fileId, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await filesService.DeleteAsync(encryptedItemId, fileId, (ClaimsIdentity)User.Identity, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Change the name of a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="newName">The new name of the file.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        [HttpPut]
        [Route("~/api/v3/items/{encryptedItemId}/files/{fileId:int}/rename/{newName}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RenameFileAsync(string encryptedItemId, int fileId, string newName, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await filesService.RenameAsync(encryptedItemId, fileId, newName, (ClaimsIdentity)User.Identity, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Change the title/description of a file.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="newTitle">The new title/description of the file.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        [HttpPut]
        [Route("~/api/v3/items/{encryptedItemId}/files/{fileId:int}/title/{newTitle}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateFileTitleAsync(string encryptedItemId, int fileId, string newTitle, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await filesService.UpdateTitleAsync(encryptedItemId, fileId, newTitle, (ClaimsIdentity)User.Identity, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Update the extra data of a file. This is data such as alt texts for different languages.
        /// </summary>
        /// <param name="encryptedItemId">The encrypted ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="extraData">The new information of the file.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        [HttpPut]
        [Route("~/api/v3/items/{encryptedItemId}/files/{fileId:int}/extra-data")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateExtraDataAsync(string encryptedItemId, int fileId, [FromBody]FileExtraDataModel extraData, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await filesService.UpdateExtraDataAsync(encryptedItemId, fileId, extraData, (ClaimsIdentity)User.Identity, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the ordering of a file.
        /// </summary>
        /// <param name="itemId">The ID of the item the file is linked to.</param>
        /// <param name="fileId">The ID of the file to update.</param>
        /// <param name="previousPosition">The current ordering number.</param>
        /// <param name="newPosition">The new ordering number.</param>
        /// <param name="propertyName">The name of the property that contains the file upload.</param>
        /// <param name="itemLinkId">Optional: If the file should be added to a link between two items, instead of an item, enter the ID of that link here.</param>
        /// <param name="entityType">Optional: When uploading a file for an item that has a dedicated table, enter the entity type name here so that we can see which table we need to add the file to.</param>
        /// <param name="linkType">Optional: When uploading a file for an item link that has a dedicated table, enter the link type here so that we can see which table we need to add the file to.</param>
        [HttpPut]
        [Route("~/api/v3/items/{itemId:int}/files/{fileId:int}/ordering")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOrderingAsync(ulong itemId, int fileId, [FromQuery]int previousPosition, [FromQuery]int newPosition, [FromQuery]string propertyName, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await filesService.UpdateOrderingAsync((ClaimsIdentity)User.Identity, fileId, previousPosition, newPosition, itemId, propertyName, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }
    }
}