namespace Api.Modules.Items.Models;

/// <summary>
/// A model for returning search results.
/// </summary>
public class SearchResponseModel
{
    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets or sets the encrypted ID of the item.
    /// </summary>
    public string EncryptedId { get; set; }

    /// <summary>
    /// Gets or sets the name of the item.
    /// </summary>
    public string Name { get; set; }
}