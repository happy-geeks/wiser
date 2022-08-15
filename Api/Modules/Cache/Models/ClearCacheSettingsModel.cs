using System.Collections.Generic;

namespace Api.Modules.Cache.Models;

/// <summary>
/// A model with settings for clearing the cache of a GCL website.
/// </summary>
public class ClearCacheSettingsModel
{
    /// <summary>
    /// Gets or sets the URL to the GCL website.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the cache areas to clear.
    /// </summary>
    public List<string> Areas { get; set; } = new();
}