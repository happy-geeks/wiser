using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model of the grid setting of a module.
    /// </summary>
    public class ModuleGridSettings
    {
        /// <summary>
        /// The id ov the module.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// The Custom query.
        /// </summary>
        public string CustomQuery { get; set; }

        /// <summary>
        /// The count query.
        /// </summary>
        public string CountQuery { get; set; }

        /// <summary>
        /// The options of the grid.
        /// </summary>
        public string GridOptions { get; set; }

        /// <summary>
        /// The id of the div the grid will be placed.
        /// </summary>
        public string GridDivId { get; set; }

        /// <summary>
        /// The name of the grid.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The read options of the grid
        /// </summary>
        public string GridReadOptions { get; set; }
    }
}
