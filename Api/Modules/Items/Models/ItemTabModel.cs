using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Api.Modules.Items.Models;

/// <summary>
/// A model for a tab on an item in Wiser.
/// </summary>
public class ItemTabModel
{
    /// <summary>
    /// Gets or sets the name of the tab.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the HTML for this tab, this contains all fields.
    /// </summary>
    public string HtmlTemplate => HtmlTemplateBuilder.ToString();

    /// <summary>
    /// Gets the javascript for this tab, to initialize all fields on this tab when the tab is selected.
    /// </summary>
    public string ScriptTemplate => ScriptTemplateBuilder.ToString();

    /// <summary>
    /// Gets or sets the groups on this tab.
    /// </summary>
    [JsonIgnore]
    public List<ItemGroupModel> Groups { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="StringBuilder"/> for generating the HTML for this tab.
    /// </summary>
    [JsonIgnore]
    public StringBuilder HtmlTemplateBuilder { get; set; } = new();

    /// <summary>
    /// Gets or sets the <see cref="StringBuilder"/> for generating the javascript for this tab.
    /// </summary>
    [JsonIgnore]
    public StringBuilder ScriptTemplateBuilder { get; set; } = new();
}