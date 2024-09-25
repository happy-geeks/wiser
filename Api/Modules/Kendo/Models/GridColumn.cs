using System.Collections.Generic;

namespace Api.Modules.Kendo.Models
{
    /// <summary>
    /// A model with settings for a column in a grid.
    /// </summary>
    public class GridColumn
    {
        /// <summary>
        /// The field to which the column is bound. The value of this field is displayed in the column's cells during data binding.
        /// Only columns that are bound to a field can be sortable or filterable.
        /// The field name should be a valid Javascript identifier and should contain only alphanumeric characters (or "$" or "_"), and may not start with a digit.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// The text that is displayed in the column header cell. If not set the field is used.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The width of the column. Numeric values are treated as pixels. For more important information, please refer to https://docs.telerik.com/kendo-ui/controls/data-management/grid/appearance/overview#column-widths.
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// The format that is applied to the value before it is displayed.
        /// Takes the form "{0:format}" where "format" is a standard number format (https://docs.telerik.com/kendo-ui/api/javascript/kendo#standard-number-formats), custom number format (https://docs.telerik.com/kendo-ui/api/javascript/kendo#standard-number-formats), standard date format (https://docs.telerik.com/kendo-ui/api/javascript/kendo#standard-date-formats) or a custom date format (https://docs.telerik.com/kendo-ui/api/javascript/kendo#custom-date-formats).
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// The configuration of the column command(s). If set the column would display a button for every command. Commands can be custom or built-in ("edit" or "destroy").
        /// The "edit" built-in command switches the current table row in edit mode.
        /// The "destroy" built-in command removes the data item to which the current table row is bound.
        /// Custom commands are supported by specifying the click option.
        /// </summary>
        public object Command { get; set; }

        /// <summary>
        /// The template which renders the column content. The grid renders table rows (&lt;tr&gt;) which represent the data source items.
        /// Each table row consists of table cells (&lt;td&gt;) which represent the grid columns.
        /// By default the HTML-encoded value of the field is displayed in the column.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Provides a way to specify a custom editing UI for the column. Use the container parameter to create the editing UI.
        /// </summary>
        public string Editor { get; set; }

        /// <summary>
        /// An array of values that will be displayed instead of the bound value. Each item in the array must have a text and value fields.
        /// </summary>
        public List<DataSourceItemModel> Values { get; set; }

        /// <summary>
        /// The query for getting data for custom fields, such as dropdown fields.
        /// </summary>
        public string DataQuery { get; set; }

        /// <summary>
        /// The items that should be used as the data source for the custom editor, such as dropdown fields.
        /// </summary>
        public List<DataSourceItemModel> DataItems { get; set; }

        /// <summary>
        /// This tells Newtonsoft (JSON.NET) that the property <see cref="DataQuery"/> should never be serialized.
        /// </summary>
        public bool ShouldSerializeDataQuery() { return false; }

        /// <summary>
        /// This tells Newtonsoft (JSON.NET) that the property <see cref="DataItems"/> should never be serialized.
        /// </summary>
        public bool ShouldSerializeDataItems() { return false; }

        /// <summary>
        /// If set to true the grid will render a select column with checkboxes in each cell, thus enabling multi-row selection.
        /// The header checkbox allows users to select/deselect all the rows on the current page.
        /// </summary>
        public bool? Selectable { get; set; }

        /// <summary>
        /// If set to true a filter menu will be displayed for this column when filtering is enabled.
        /// If set to false the filter menu will not be displayed.
        /// It's also possible to specifically indicate how the filters should work. See the Kendo documentation for more information: https://docs.telerik.com/kendo-ui/api/javascript/ui/grid/configuration/columns.filterable
        /// By default a filter menu is displayed for all columns when filtering is enabled via the filterable option.
        /// </summary>
        public dynamic Filterable { get; set; }

        /// <summary>
        /// If set to true the column will not be displayed in the grid. By default all columns are displayed.
        /// </summary>
        public bool? Hidden { get; set; }
        
        /// <summary>
        /// If set to false the column will not be editable by the user.
        /// </summary>
        public bool? Editable { get; set; }

        /// <summary>
        /// If the column contains a progress bar, you can use this to configure it.
        /// </summary>
        public GridProgressBarColumnSettings ProgressBarSettings { get; set; }

        /// <summary>
        /// Gets or sets whether the field of this grid column is a property from wiser_itemlinkdetail, instead of wiser_itemdetail.
        /// </summary>
        public bool IsLinkProperty { get; set; }
        
        /// <summary>
        /// Gets or sets the attributes of the column.
        /// </summary>
        public AttributesModel Attributes { get; set; }
    }
}