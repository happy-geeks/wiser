using System.ComponentModel.DataAnnotations;
using Api.Modules.EntityProperties.Enums;

namespace Api.Modules.EntityProperties.Models
{
    /// <summary>
    /// A model for the property of an entity within Wiser.
    /// </summary>
    public class EntityPropertyModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module. This can be 0 if the property should be available for all modules.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the entity.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the link type if the property is used in a connection between two items.
        /// </summary>
        public int LinkType { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the tab the property is shown in.
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Gets or sets the name of the group the property is in.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the input type.
        /// </summary>
        public EntityPropertyInputTypes InputType { get; set; }

        /// <summary>
        /// Gets or sets the name to display.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the order the property is shown in.
        /// </summary>
        public int Ordering { get; set; }

        /// <summary>
        /// Gets or sets an explanation shown with the property.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Gets or sets an extended explanation shown with the property.
        /// </summary>
        public bool ExtendedExplanation { get; set; }

        /// <summary>
        /// Gets or sets the regex used for validation.
        /// </summary>
        public string RegexValidation { get; set; }

        /// <summary>
        /// Gets or sets if the property is mandatory to be filled.
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        /// Gets or sets if the property can only be read.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the width the property is shown with.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height the property is shown with.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the JSON object containing the options.
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Gets or sets the query used to get data used to fill input types like combo boxes and grids.
        /// </summary>
        public string DataQuery { get; set; }

        /// <summary>
        /// Gets or sets the query that needs to be performed when an action button is clicked.
        /// </summary>
        public string ActionQuery { get; set; }

        /// <summary>
        /// Gets or sets the query used to search within sub entities grids.
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// Gets or sets the query used for the total count for the search query.
        /// </summary>
        public string SearchCountQuery { get; set; }

        /// <summary>
        /// Gets or sets a custom query to insert an item in the sub entities grid.
        /// </summary>
        public string GridInsertQuery { get; set; }

        /// <summary>
        /// Gets or sets a custom query to update an item in the sub entities grid.
        /// </summary>
        public string GridUpdateQuery { get; set; }

        /// <summary>
        /// Gets or sets a custom query to delete an item in the sub entities grid.
        /// </summary>
        public string GridDeleteQuery { get; set; }

        /// <summary>
        /// Gets or sets the dependency op the property is it has one.
        /// </summary>
        public EntityPropertyDependencyModel DependsOn { get; set; }

        /// <summary>
        /// Gets or sets a custom javascript.
        /// </summary>
        public string CustomScript { get; set; }

        /// <summary>
        /// Gets or sets if the property also needs to be saved with a SEO value.
        /// </summary>
        public bool AlsoSaveSeoValue { get; set; }

        /// <summary>
        /// Gets or sets if the item needs to be saved immediately when the value of the property has changed.
        /// </summary>
        public bool SaveOnChange { get; set; }

        /// <summary>
        /// Gets or sets the style of the label.
        /// </summary>
        public EntityPropertyLabelStyles? LabelStyle { get; set; }

        /// <summary>
        /// Gets or sets the width of the label.
        /// </summary>
        public int LabelWidth { get; set; }

        /// <summary>
        /// Gets or sets the information for the property in the overview.
        /// </summary>
        public EntityPropertyOverviewModel Overview { get; set; }

        /// <summary>
        /// Gets or sets whether to enable aggregation for this property.
        /// </summary>
        public bool EnableAggregation { get; set; }

        /// <summary>
        /// Gets or sets the options for aggregation.
        /// </summary>
        public string AggregateOptions { get; set; }

        /// <summary>
        /// Gets or sets the access key for the field in Wiser.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the regular expression to decide whether the field should be visible in Wiser, based on the full path of this item in the tree view.
        /// This is only used when the item is loaded via the tree view, not if it's loaded in other ways.
        /// </summary>
        public string VisibilityPathRegex { get; set; }
    }
}
