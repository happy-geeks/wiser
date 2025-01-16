using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Imports.Models;

//TODO Verify comments
/// <summary>
/// A model for the combo box data for the Wiser import module.
/// </summary>
public class ComboBoxDataModel
{
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the property.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    public JObject Options { get; set; }

    /// <summary>
    /// Gets or sets the data query.
    /// </summary>
    public string DataQuery { get; set; }

    /// <summary>
    /// Gets or sets the values of the combo box.
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
}