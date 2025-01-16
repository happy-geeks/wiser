using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Api.Modules.DataSelectors.Models;

/// <summary>
/// The model for an entity property for the data selector.
/// </summary>
[JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class DataSelectorEntityPropertyModel
{
    /// <summary>
    /// Gets or sets the unique ID of the property. Will be comprised of the property name and, if export mode is enabled, the language code.
    /// </summary>
    [JsonProperty("value")]
    public string UniqueId { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity this property belongs to.
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the property.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the language code of the field. Will always be empty if export mode is not enabled.
    /// </summary>
    public string LanguageCode { get; set; }
}