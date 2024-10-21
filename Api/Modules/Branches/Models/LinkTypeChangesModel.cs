namespace Api.Modules.Branches.Models
{
    /// <inheritdoc />
    public class LinkTypeChangesModel : BranchChangesModel
    {
        /// <summary>
        /// Gets or sets the id of the link type setting.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the link type to merge.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the source item if this link type.
        /// </summary>
        public string SourceEntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the destination item if this link type.
        /// </summary>
        public string DestinationEntityType { get; set; }
    }
}