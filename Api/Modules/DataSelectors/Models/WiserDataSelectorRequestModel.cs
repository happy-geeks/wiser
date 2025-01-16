using GeeksCoreLibrary.Modules.DataSelector.Models;

namespace Api.Modules.DataSelectors.Models;

/// <summary>
/// Class that represents the query string variables for a get_items request.
/// </summary>
public class WiserDataSelectorRequestModel : DataSelectorRequestModel
{
    /// <summary>
    /// Gets or sets the encrypted ID of the data selector.
    /// </summary>
    public string EncryptedDataSelectorId { get; set; }
}