namespace Api.Modules.Imports.Models;

//TODO Verify comments
/// <summary>
/// A model for the settings of the image upload in the Wiser import module.
/// </summary>
public class ImageUploadSettingsModel
{
    /// <summary>
    /// Gets or sets the name of the property of the image.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the path for the uploaded file.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets if multiple images are allowed.
    /// </summary>
    public bool AllowMultipleImages { get; set; }
}