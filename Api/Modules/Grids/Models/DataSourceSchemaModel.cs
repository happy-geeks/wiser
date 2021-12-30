using System.Collections.Generic;

namespace Api.Modules.Grids.Models
{
    /// <summary>
    /// The data item (model) configuration.
    /// </summary>
    public class DataSourceSchemaModel
    {
        /// <summary>
        /// The value of the ID of the Model. This field is available only if the id is defined in the Model configuration.
        /// </summary>
        public string Id { get; set; } = "id";

        /// <summary>
        /// The fields of a data source.
        /// </summary>
        public Dictionary<string, FieldModel> Fields { get; set; } = new();
    }
}