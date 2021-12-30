 namespace FrontEnd.Modules.ImportExport.Models
 {
     public class ChunkMetaDataModel
     {
         public string UploadUid { get; set; }

         public string FileName { get; set; }

         public string ContentType;

         public long ChunkIndex;

         public long TotalChunks;

         public long TotalFileSize;
     }
 }