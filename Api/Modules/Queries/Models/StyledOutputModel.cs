namespace Api.Modules.Queries.Models
{
    /// <summary>
    /// A model for a styledoutput within Wiser.
    /// </summary>
    public class StyledOutputModel
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the format that gets inserted before the item list starts
        /// </summary>
        public string FormatBegin { get; set; }
        
        /// <summary>
        /// Gets or sets the format get gets used per item entry
        /// </summary>
        public string FormatItem { get; set; }
        
        /// <summary>
        /// Gets or sets the format that gets inserted at the end of the item list
        /// </summary>
        public string FormatEnd { get; set; }
        
        /// <summary>
        /// Gets or sets the format that gets used instead of format_being, format_item and format_end when no results are given
        /// </summary>
        public string FormatEmpty { get; set; }
        
        /// <summary>
        /// Gets or sets the query id that is used to retrieve data, this id should match with a query from the wiser_query table
        /// </summary>
        public int QueryId { get; set; }
        
        /// <summary>
        /// Gets or sets the expected return type, for now only JSON is supported but in the future this can be extended
        /// </summary>
        public string ReturnType { get; set; }
        
        /// <summary>
        /// Gets or Sets the option field, this is a JSON style option string field that gets parsed for every run
        /// </summary>
        public string Options { get; set; }
    }
}
