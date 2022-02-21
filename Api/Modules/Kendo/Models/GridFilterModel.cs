using System.Collections.Generic;

namespace Api.Modules.Kendo.Models
{
    /// <summary>
    /// A model for filter options for grids.
    /// </summary>
    public class GridFilterModel
    {
        /// <summary>
        /// The operator to use for filtering. Possible values:
        /// rq, neq, startswith, contains, doesnotcontain, endswith, isnull, isnotnull, isempty, isnotempty, lt, gt, lte and gte
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the field to filter on.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the value to filter for.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the filter logic (and/or).
        /// </summary>
        public string Logic { get; set; }

        /// <summary>
        /// Gets or sets any sub filters.
        /// </summary>
        public List<GridFilterModel> Filters { get; set; }
    }
}