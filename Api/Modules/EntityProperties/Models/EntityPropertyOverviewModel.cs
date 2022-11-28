namespace Api.Modules.EntityProperties.Models
{
    //TODO Verify comments
    /// <summary>
    /// A model for the entity property used in the overview within Wiser.
    /// </summary>
    public class EntityPropertyOverviewModel
    {
        /// <summary>
        /// Get or sets if the entity property is visible in the overview.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public int Width { get; set; }
    }
}
