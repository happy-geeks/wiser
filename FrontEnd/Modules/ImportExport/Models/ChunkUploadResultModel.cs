namespace FrontEnd.Modules.ImportExport.Models;

public class ChunkUploadResultModel
{
    public bool Uploaded { get; set; }

    public string FileUid { get; set; }

    public string Filename { get; set; } = "";

    public string FilePath { get; set; } = "";
}