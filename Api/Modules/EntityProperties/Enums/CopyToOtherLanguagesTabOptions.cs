namespace Api.Modules.EntityProperties.Enums;

/// <summary>
/// Enumeration with options for which tab(s) to add fields to when copying them to other languages.
/// </summary>
public enum CopyToOtherLanguagesTabOptions
{
    /// <summary>
    /// Add the field to the general tab.
    /// </summary>
    General,
    /// <summary>
    /// Add the field to the tab with the language code.
    /// </summary>
    LanguageCode,
    /// <summary>
    /// Add the field to the tab with the language code.
    /// </summary>
    LanguageName
}