using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Api.Modules.EntityProperties.Enums;

/// <summary>
/// An enum containing all possible orientations for the layout of the properties in a property group
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EntityGroupOrientation
{
    /// <summary>
    /// The properties are stacked horizontally, only going on the next line if there is no more space
    /// </summary>
    [EnumMember(Value = "Horizontal")]
    Horizontal,

    /// <summary>
    /// The properties are stacked vertically
    /// </summary>
    [EnumMember(Value = "Vertical")]
    Vertical
}