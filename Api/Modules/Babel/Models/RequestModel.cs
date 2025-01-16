using System;
using System.Collections.Generic;

namespace Api.Modules.Babel.Models;

/// <summary>
/// A model for a Babel conversion request.
/// </summary>
public class RequestModel
{
    /// <summary>
    /// Gets or sets the content to convert with Babel.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the conversion options.
    /// </summary>
    public SortedList<string, object> Options { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
}