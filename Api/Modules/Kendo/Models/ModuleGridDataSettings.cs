namespace Api.Modules.Kendo.Models
{    
    /// <summary>
    /// A model for the settings of a grid
    /// </summary>
    public class ModuleGridDataSettings
    {
        /// <summary>
        /// The query to get the data of the grid
        /// </summary>
        public string CustomQuery { get; set; }

        /// <summary>
        /// The count query for the grid
        /// </summary>
        public string CountQuery { get; set; }

        /// <summary>
        /// The options for the grid
        /// </summary>
        public string GridOptions { get; set; }

        /// <summary>
        /// The read options of the grid
        /// </summary>
        public GridReadOptionsModel GridReadOptions { get; set; }


    }
}

