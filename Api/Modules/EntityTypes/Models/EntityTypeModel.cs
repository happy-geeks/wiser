using System.Collections.Generic;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace Api.Modules.EntityTypes.Models
{
    /// <summary>
    /// The model for a Wiser 2.0 entity type.
    /// An item in Wiser always has an entity type, this model contains information about such entity types.
    /// </summary>
    public class EntityTypeModel
    {
        /// <summary>
        /// Gets or sets the technical name.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module that this entity type belongs to.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets a list of allowed child entity types.
        /// </summary>
        public List<string> AcceptedChildTypes { get; set; }

        /// <summary>
        /// Gets or sets the icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets add icon
        /// </summary>
        public string IconAdd { get; set; }

        /// <summary>
        /// Gets or sets the expanded icon
        /// </summary>
        public string IconExpanded { get; set; }

        /// <summary>
        /// Gets or sets query after insert
        /// </summary>
        public string QueryAfterInsert { get; set; }

        /// <summary>
        /// Gets or sets query after update
        /// </summary>
        public string QueryAfterUpdate { get; set; }

        /// <summary>
        /// Gets or sets query before update
        /// </summary>
        public string QueryBeforeUpdate { get; set; }

        /// <summary>
        /// Gets or sets the query before delete
        /// </summary>
        public string QueryBeforeDelete { get; set; }

        /// <summary>
        /// Gets or sets the color
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the id of an API executed after insert
        /// </summary>
        public int? ApiAfterInsert { get; set; }

        /// <summary>
        /// Gets or sets the id of an API executed after update
        /// </summary>
        public int? ApiAfterUpdate { get; set; }

        /// <summary>
        /// Gets or sets the id of an API executed before update
        /// </summary>
        public int? ApiBeforeUpdate { get; set; }

        /// <summary>
        /// Gets or sets the id of an API executed before delete
        /// </summary>
        public int? ApiBeforeDelete { get; set; }

        /// <summary>
        /// Gets or sets the user friendly name
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the default ordering 
        /// </summary>
        public string DefaultOrdering { get; set; }

        /// <summary>
        /// Gets or sets title is visible when entity is opened
        /// </summary>
        public bool ShowTitleField { get; set; }

        /// <summary>
        /// Gets or sets item is visible in search results
        /// </summary>
        public bool ShowInSearch { get; set; }

        /// <summary>
        /// Gets or sets entity type is visible in overview tab
        /// </summary>
        public bool ShowOverviewTab { get; set; }
        
        public bool ShowInTreeView { get; set; }

        public bool SaveHistory { get; set; }

        public bool SaveTitleAsSeo { get; set; }
    }
}