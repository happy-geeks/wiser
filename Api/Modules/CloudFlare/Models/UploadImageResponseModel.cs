using System;
using System.Collections.Generic;

namespace Api.Modules.CloudFlare.Models;

/// <summary>
/// This inner class describes an object for the result of an image being uploaded
/// </summary>
public class UploadImageResult
{
    /// <summary>
    /// The ID of the result
    /// </summary>
    public string Id { get; set; }
        
    /// <summary>
    /// The filename of the result
    /// </summary>
    public string Filename { get; set; }
        
    /// <summary>
    /// The date and time of the result
    /// </summary>
    public DateTime Uploaded { get; set; }
        
    /// <summary>
    /// Checks if Signed URLs are needed
    /// </summary>
    public bool RequireSignedURLs { get; set; }
        
    /// <summary>
    /// List of the different Variants in the result
    /// </summary>
    public List<string> Variants { get; set; }
}

/// <summary>
/// A class describing an object for the response of an Image being uploaded
/// </summary>
public class UploadImageResponseModel
{
    /// <summary>
    /// The result within the response
    /// </summary>
    public UploadImageResult Result { get; set; }
        
    /// <summary>
    /// An object with extra info from the result in the response
    /// </summary>
    public object ResultInfo { get; set; }
        
    /// <summary>
    /// Checks if the response is successful
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