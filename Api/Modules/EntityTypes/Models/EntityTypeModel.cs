namespace Api.Modules.EntityTypes.Models
{
    /// <summary>
    /// The model for a Wiser entity type.
    /// An item in Wiser always has an entity type, this model contains information about such entity types.
    /// </summary>
    public class EntityTypeModel
    {
        /// <summary>
        /// Gets or sets the technical name.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module that this entity type belongs to.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the module that this entity type belongs to.
        /// </summary>
        public string ModuleName { get; set; }
    }
}