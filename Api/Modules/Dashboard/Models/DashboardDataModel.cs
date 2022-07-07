using System;
using System.Collections.Generic;

namespace Api.Modules.Dashboard.Models;

public class DashboardDataModel
{
    /// <summary>
    /// Gets or sets a list of entities with their usage. This list will contain the 8 entities with the highest amount of entities.
    /// </summary>
    public List<EntityUsageModel> EntityUsage { get; set; }

    /// <summary>
    /// Gets or sets the amount of times the top 10 users have logged in.
    /// </summary>
    public int UserLoginCountTop10 { get; set; }

    /// <summary>
    /// Gets or sets the amount of times all other users have logged in (users that aren't in the top 10).
    /// </summary>
    public int UserLoginCountOther { get; set; }

    /// <summary>
    /// Gets or sets the amount of time the top 10 users have spent logged in.
    /// </summary>
    public TimeSpan UserLoginTimeTop10 { get; set; }

    /// <summary>
    /// Gets or sets the amount of time all other users have spent logged in (users that aren't in the top 10).
    /// </summary>
    public TimeSpan UserLoginTimeOther { get; set; }
}