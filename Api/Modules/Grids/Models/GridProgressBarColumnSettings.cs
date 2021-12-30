using System.Collections.Generic;

namespace Api.Modules.Grids.Models
{
    /// <summary>
    /// A model with settings for a progress bar inside a grid.
    /// </summary>
    public class GridProgressBarColumnSettings
    {
        /// <summary>
        /// Gets or sets the maximum progress/value of the progress bar.
        /// </summary>
        public int MaxProgress { get; set; }

        /// <summary>
        /// Gets or sets the different colors for different progress values.
        /// </summary>
        public List<ProgressBarColor> ProgressColors { get; set; }
    }
}