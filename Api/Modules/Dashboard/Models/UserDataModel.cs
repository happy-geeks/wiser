using System;

namespace Api.Modules.Dashboard.Models;

public class UserDataModel
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the amount of times the user has logged into Wiser.
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// Gets or sets the time the user has spent logged in.
    /// </summary>
    public TimeSpan LoginTime { get; set; }
}