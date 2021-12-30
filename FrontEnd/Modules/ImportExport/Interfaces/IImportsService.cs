using System.Collections.Generic;
using System.Threading.Tasks;
using FrontEnd.Modules.ImportExport.Models;
using Microsoft.AspNetCore.Http;

namespace FrontEnd.Modules.ImportExport.Interfaces
{
    /// <summary>
    /// Service for handling things for the import module.
    /// </summary>
    public interface IImportsService
    {
        /// <summary>
        /// Handle the upload of a feel file (CSV) upload.
        /// </summary>
        /// <param name="formCollection">The uploaded files from the request.</param>
        /// <param name="uploadsDirectory">The directory on server where the files should be temporarily saved.</param>
        /// <returns>A <see cref="FeedFileUploadResultModel"/> with information about the contents of the uploaded file.</returns>
        Task<FeedFileUploadResultModel> HandleFeedFileUploadAsync(IFormCollection formCollection, string uploadsDirectory);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formCollection">The uploaded files from the request.</param>
        /// <param name="uploadsDirectory">The directory on server where the files should be temporarily saved.</param>
        /// <returns>A <see cref="ChunkUploadResultModel"/> with information about the contents of the uploaded file.</returns>
        Task<ChunkUploadResultModel> HandleImagesFileUploadAsync(IFormCollection formCollection, string uploadsDirectory);
    }
}
