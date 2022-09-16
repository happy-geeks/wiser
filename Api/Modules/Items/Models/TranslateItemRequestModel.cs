using System.Collections.Generic;

namespace Api.Modules.Items.Models;

/// <summary>
/// A model with parameters for the method for translating all fields for a Wiser item.
/// </summary>
public class TranslateItemRequestModel
{
    /// <summary>
    /// Gets or sets the entity type of the Wiser item.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the source language code to translate from. All fields with this language code, that contain a value, will be translated.
    /// </summary>
    public string SourceLanguageCode { get; set; }

    /// <summary>
    /// Gets or sets the language codes to translate into. Leave empty or null to translate into all configured languages.
    /// </summary>
    public List<string> TargetLanguageCodes { get; set; }
}