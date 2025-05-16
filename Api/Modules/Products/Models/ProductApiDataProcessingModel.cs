namespace Api.Modules.Products.Models;

/// <summary>
/// A container for processing data as we parse it, so we can save results or group operations together.
/// </summary>
public class ProductApiDataProcessingModel
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public int DataRowIndex { get; set; }

    /// <summary>
    /// Gets or sets the wiser id related to this product.
    /// </summary>
    public ulong WiserId { get; set; }

    /// <summary>
    /// Gets or sets the version number related to this product.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or Sets the old hash.
    /// </summary>
    public string OldHash { get; set; }

    /// <summary>
    /// Gets or Sets the new hash.
    /// </summary>
    public string NewHash { get; set; }

    /// <summary>
    /// Gets or Sets the published env.
    /// </summary>
    public int PublishedEnvironment { get; set; }

    /// <summary>
    /// Gets or Sets the Type we use for the datasource.
    /// </summary>
    public int DatasourceType { get; set; }

    /// <summary>
    /// Gets or Sets the static text when used.
    /// </summary>
    public string StaticText { get; set; }

    /// <summary>
    /// Gets or Sets the query id when used.
    /// </summary>
    public int QueryId { get; set; }

    /// <summary>
    /// Gets or Sets the styled output id when used.
    /// </summary>
    public int StyledOutputId { get; set; }

    /// <summary>
    /// Gets or Sets the styled output id when used.
    /// </summary>
    public ulong ApiEntryId { get; set; }

    /// <summary>
    /// Gets or sets the output aka the results that came out of the query, styledoutput or static output.
    /// </summary>
    public string Output { get; set; }

}