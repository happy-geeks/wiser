using System;

namespace Api.Modules.Dashboard.Models;

public class ServiceLog
{
    /// <summary>
    /// Gets or sets the ID of the log.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the level the log has been written on.
    /// </summary>
    public string Level { get; set; }

    /// <summary>
    /// Gets or sets the scope that generated the log.
    /// </summary>
    public string Scope { get; set; }

    /// <summary>
    /// Gets or sets the source of the log.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the name of the configuration that generated the log.
    /// </summary>
    public string Configuration { get; set; }

    /// <summary>
    /// Gets or sets the time ID of the run scheme that generated the log.
    /// </summary>
    public int TimeId { get; set; }

    /// <summary>
    /// Gets or sets the order of the action that generated the log.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the date the log has been added on.
    /// </summary>
    public DateTime AddedOn { get; set; }

    /// <summary>
    /// Gets or sets the content of the log.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets if the log is from a test environment.
    /// </summary>
    public bool IsTest { get; set; }
}