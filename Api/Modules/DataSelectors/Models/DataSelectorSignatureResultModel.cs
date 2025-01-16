namespace Api.Modules.DataSelectors.Models;

//TODO Verify comments
/// <summary>
/// The model for a Wiser data selector signature result.
/// </summary>
public class DataSelectorSignatureResultModel
{
    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; }

    /// <summary>
    /// Gets or sets the extra query string.
    /// </summary>
    public string ExtraQueryString { get; set; }
}