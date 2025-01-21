namespace Api.Modules.Branches.Models;

/// <inheritdoc />
public class EntityChangesModel : BranchChangesModel
{
    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; }
}