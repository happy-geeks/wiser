namespace Api.Modules.Grids.Models
{
    /// <summary>
    /// A model with field settings for a Kendo data source.
    /// </summary>
    public class FieldModel
    {
        /// <summary>
        /// Gets or sets the field type (string, int etc).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether this field should be editable.
        /// </summary>
        public bool Editable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this field can be <see langword="null"/>.
        /// </summary>
        public bool Nullable { get; set; } = true;
    }
}