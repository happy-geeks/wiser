using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontEnd.Modules.ImportExport.Interfaces;
using FrontEnd.Modules.ImportExport.Models;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace FrontEnd.Modules.ImportExport.Services
{
    public class ImportsService : IImportsService
    {
        private readonly IExcelService excelService;
        
        private const uint ImportLimit = 1000000;

        public ImportsService(IExcelService excelService)
        {
            this.excelService = excelService;
        }

        /// <inheritdoc />
        public async Task<FeedFileUploadResultModel> HandleFeedFileUploadAsync(IFormCollection formCollection, string uploadsDirectory)
        {
            if (formCollection?.Files == null || !formCollection.Files.Any())
            {
                return null;
            }

            // For now only one file is supported.
            var uploadedFile = formCollection.Files[0];
            var uploadedFilename = Path.GetFileName(uploadedFile.FileName);
            var filePath = Path.Combine(uploadsDirectory, uploadedFilename);

            // Save the file to disc.
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(fileStream);
            }
            
            var uploadResult = new FeedFileUploadResultModel
            {
                ImportLimit = ImportLimit,
                Filename = filePath
            };

            if (filePath.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase))
            {
                uploadResult.Columns = excelService.GetColumnNames(filePath).ToArray();
                // One line is subtracted because the header line doesn't count.
                uploadResult.RowCount = Convert.ToUInt32(excelService.GetRowCount(filePath) - 1);
            }
            else
            {
                byte[] fileBytes;
                await using (var memoryStream = new MemoryStream())
                {
                    await uploadedFile.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                switch (fileBytes.Length)
                {
                    case 0:
                        return uploadResult;
                    case >= 3:
                        // If the file has the UTF-8 BOM, it can't properly detect if a column called "id" exists if the first column is the id column.
                        fileBytes = RemoveUtf8BomBytes(fileBytes);
                        break;
                }

                // Turn the bytes into a string.
                var fileContents = Encoding.UTF8.GetString(fileBytes);

                var linesCounted = 0U;
                using (var stringReader = new StringReader(fileContents))
                {
                    using (var reader = new TextFieldParser(stringReader))
                    {
                        reader.Delimiters = new[] {";"};
                        reader.TextFieldType = FieldType.Delimited;
                        reader.HasFieldsEnclosedInQuotes = true;

                        uploadResult.Columns = reader.ReadFields();

                        // Need to read through the entire document once to determine how many lines the document contains.
                        // Splitting on newlines isn't good enough as it's valid in CSV documents for lines to contain line breaks.
                        while (!reader.EndOfData)
                        {
                            reader.ReadFields();
                            linesCounted += 1U;
                        }
                    }
                }

                // One line is subtracted because the header line doesn't count.
                uploadResult.RowCount = linesCounted;
            }

            if (uploadResult.Columns != null && uploadResult.Columns.Contains("id", StringComparer.OrdinalIgnoreCase))
            {
                uploadResult.Columns = uploadResult.Columns.Where(c => !c.Equals("id", StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            return uploadResult;
        }

        /// <inheritdoc />
        public async Task<ChunkUploadResultModel> HandleImagesFileUploadAsync(IFormCollection formCollection, string uploadsDirectory)
        {
            if (formCollection?.Files == null || !formCollection.Files.Any())
            {
                return null;
            }

            var metaData = formCollection.ContainsKey("metadata") ? formCollection["metadata"].ToString() : null;
            if (String.IsNullOrWhiteSpace(metaData))
            {
                return null;
            }
            
            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }
 
            var chunkMetaData = JsonConvert.DeserializeObject<ChunkMetaDataModel>(formCollection["metadata"].ToString());
            if (chunkMetaData == null)
            {
                return null;
            }

            var uploadedFile = formCollection.Files[0];
            var uploadedFilename = $"{Path.GetFileName(chunkMetaData.FileName)}_{chunkMetaData.ChunkIndex}";
            var basePath = Path.Combine(uploadsDirectory, "chunks");
            var filePath = Path.Combine(basePath, uploadedFilename);

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            // Save the file to disc.
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(fileStream);
            }
            
            var allChunksUploaded = chunkMetaData.TotalChunks - 1 <= chunkMetaData.ChunkIndex;
            if (!allChunksUploaded)
            {
                return new ChunkUploadResultModel { Uploaded = false, FileUid = chunkMetaData.UploadUid, Filename = chunkMetaData.FileName, FilePath = Path.Combine(uploadsDirectory, Path.GetFileName(chunkMetaData.FileName)) };
            }

            await using (var memoryStream = new MemoryStream())
            {
                // Iterate through all chunk files so the original file can be created.
                for (var i = 0; i <= chunkMetaData.TotalChunks - 1; i++)
                {
                    var filename = Path.Combine(basePath, $"{chunkMetaData.FileName}_{i}");
                    var fileBytes = await File.ReadAllBytesAsync(filename);
                    memoryStream.Write(fileBytes, 0, fileBytes.Length);
                }

                // Create the final file in the uploads directory.
                await File.WriteAllBytesAsync(Path.Combine(uploadsDirectory, chunkMetaData.FileName), memoryStream.ToArray());

                // Iterate again so the chunks can be deleted.
                for (var i = 0; i <= chunkMetaData.TotalChunks - 1; i++)
                {
                    var filename = Path.Combine(basePath, $"{chunkMetaData.FileName}_{i}");
                    File.Delete(filename);
                }
            }

            return new ChunkUploadResultModel { Uploaded = true, FileUid = chunkMetaData.UploadUid, Filename = chunkMetaData.FileName, FilePath = Path.Combine(uploadsDirectory, chunkMetaData.FileName) };
        }
        
        /// <summary>
        /// Checks if the first three bytes of a byte array are the same as the UTF-8 BOM. If they are, they are removed
        /// This will return the original array if there are no BOM bytes
        /// </summary>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        private static byte[] RemoveUtf8BomBytes(byte[] fileBytes)
        {
            var utf8BomBytes = new byte[] { 0xEF, 0xBB, 0xBF };
            if (fileBytes.Length < 3 || !fileBytes.Take(3).SequenceEqual(utf8BomBytes))
            {
                return fileBytes;
            }

            // File has UTF-8 BOM bytes, remove them.
            var newByteArray = new byte[fileBytes.Length - 1 + 1];
            Array.Copy(fileBytes, newByteArray, fileBytes.Length);
            newByteArray = newByteArray.Skip(3).ToArray();

            return newByteArray;
        }
    }
}
