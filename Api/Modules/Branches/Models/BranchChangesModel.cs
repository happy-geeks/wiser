namespace Api.Modules.Branches.Models;

/// <summary>
/// A base model for changes of an entity type or any kind of wiser settings that can be merged into the main/original branch.
/// </summary>
public class BranchChangesModel
{
    /// <summary>
    /// Gets or sets the display name for this type to be shown to the user in Wiser.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the amount of new items that have been created.
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    /// Gets or sets the amount of items that have been deleted.
    /// </summary>
    public int Deleted { get; set; }

    /// <summary>
    /// Gets or sets the amount of items that have been updated.
    /// </summary>
    public int Updated { get; set; }
}