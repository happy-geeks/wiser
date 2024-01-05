namespace Api.Modules.Items.Models;

/// <summary>
/// A model for defining the search request.
/// </summary>
public class SearchRequestModel
{
    /// <summary>
    /// Gets or sets the entity type of items to search for.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the search value.
    /// </summary>
    public string SearchValue { get; set; }

    /// <summary>
    /// Gets or sets whether to look in the title of items while searching. Default value is true.
    /// </summary>
    public bool SearchInTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to search for all items of the entity type (true) or only the children of the given parent Id (false).
    /// </summary>
    public bool SearchEverywhere { get; set; }

    /// <summary>
    /// Gets or sets the fields to search in.
    /// </summary>
    public string SearchFields { get; set; }
}