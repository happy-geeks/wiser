namespace Api.Modules.Items.Models;

/// <summary>
/// A model for the details of an item, for item filtering endpoints.
/// </summary>
public class WiserItemDetailInputModel
{
    /// <summary>
    /// The key of the detail/property to filter on. This is the name of the property/field in Wiser.
    /// By default, this should be the internal property name, such as "long_description".
    /// If you set "UseFriendlyPropertyNames" to true in the query, you can use the display name of the field, such as "Long Description".
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The value of the detail/property to filter on.
    /// This is an exact match filter, but it is case-insensitive.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// The language code of the detail/property to filter on.
    /// If the field doesn't use language codes, or if you want to check the value in all languages, you can leave this empty.
    /// </summary>
    public string LanguageCode { get; set; }

    /// <summary>
    /// The group name of the detail/property to filter on.
    /// Some fields are grouped together in Wiser. If you want to filter on a specific group, you can use this.
    /// </summary>
    public string GroupName { get; set; }
}