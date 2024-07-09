using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Api.Modules.Items.Models
{
    /// <summary>
    /// A model for a tab on an item in Wiser.
    /// </summary>
    public class ItemGroupModel
    {
        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the width of the group in percentage.
        /// </summary>
        public int Width { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum width of the group in pixels.
        /// </summary>
        public int MinimumWidth { get; set; } = 0;

        /// <summary>
        /// Gets or sets the stack orientation of the items in the group.
        /// Options are "Horizontal" or "Vertical"
        /// </summary>
        public string Orientation { get; set; } = "Horizontal";

        /// <summary>
        /// Gets or sets the <see cref="StringBuilder"/> for generating the HTML for this group.
        /// </summary>
        [JsonIgnore]
        public StringBuilder HtmlTemplateBuilder { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the <see cref="StringBuilder"/> for generating the javascript for this group.
        /// </summary>
        [JsonIgnore]
        public StringBuilder ScriptTemplateBuilder { get; set; } = new();
    }
}