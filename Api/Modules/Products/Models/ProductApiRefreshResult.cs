namespace Api.Modules.Products.Models;

/// <summary>
/// Result object for product API refresh operation.
/// </summary>
public class ProductApiRefreshResult
{
    /// <summary>
    /// Constructor for ProductApiRefreshResult.
    /// </summary>
    /// <param name="newlyCreatedVersionCount"></param>
    /// <param name="noUpdateCount"></param>
    /// <param name="isDone"></param>
    public ProductApiRefreshResult(int newlyCreatedVersionCount, int noUpdateCount, bool isDone)
    {
        NewlyCreatedVersionCount = newlyCreatedVersionCount;
        NoUpdateCount = noUpdateCount;
        IsDone = isDone;
    }

    /// <summary>
    /// The number of items that spawned a new version during the refresh operation.
    /// </summary>
    public int NewlyCreatedVersionCount { get; set; }

    /// <summary>
    /// The number of items that did not require an update during the refresh operation.
    /// </summary>
    public int NoUpdateCount { get; set; }

    /// <summary>
    /// Indicates whether the refresh operation is complete or if there are more items to process.
    /// </summary>
    public bool IsDone { get; set; }
}