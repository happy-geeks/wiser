using System.Collections.Generic;

namespace Api.Modules.CloudFlare.Models;

/// <summary>
/// This inner class describes an object for a DirectUploadResult
/// </summary>
public class DirectUploadResult
{
    /// <summary>
    /// The ID of the DirectUploadResult
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The Upload URL of the DirectUploadResult
    /// </summary>
    public string UploadURL { get; set; }
}

/// <summary>
/// This class describes an object for a DirectUploadResponseModel
/// </summary>
public class DirectUploadResponseModel
{
    /// <summary>
    /// The Result of the DirectUploadResponse
    /// </summary>
    public DirectUploadResult Result { get; set; }

    /// <summary>
    /// Extra info from the Result as an object
    /// </summary>
    public object ResultInfo { get; set; }

    /// <summary>
    /// A boolean to check if the response was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The errors of the response
    /// </summary>
    public List<object> Errors { get; set; }

    /// <summary>
    /// The messages of the response
    /// </summary>
    public List<object> Messages { get; set; }
}