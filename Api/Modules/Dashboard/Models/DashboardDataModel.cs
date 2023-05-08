using System.Collections.Generic;
using Api.Modules.EntityTypes.Models;

namespace Api.Modules.Dashboard.Models;

/// <summary>
/// This class describes an object for Data in an Dashboard
/// </summary>
public class DashboardDataModel
{
    /// <summary>
    /// Gets or sets a list of entities with their usage. This list will contain the 8 entities with the highest amount of entities.
    /// </summary>
    public Dictionary<string, List<ItemsCountModel>> Items { get; set; }

    /// <summary>
    /// Gets or sets the entities data. This is a count of the total amount of items for these entities, as well as the
    /// module ID and icon that is associated with the entities.
    /// </summary>
    public Dictionary<string, List<EntityTypeModel>> Entities { get; set; }

    /// <summary>
    /// Gets or sets the amount of times the top 10 users have logged in.
    /// </summary>
    public int UserLoginCountTop10 { get; set; }

    /// <summary>
    /// Gets or sets the amount of times all other users have logged in (users that aren't in the top 10).
    /// </summary>
    public int UserLoginCountOther { get; set; }

    /// <summary>
    /// Gets or sets the amount of time in seconds the top 10 users have spent logged in.
    /// </summary>
    public long UserLoginActiveTop10 { get; set; }

    /// <summary>
    /// Gets or sets the amount of time in seconds all other users have spent logged in (users that aren't in the top 10).
    /// </summary>
    public long UserLoginActiveOther { get; set; }

    /// <summary>
    /// Gets or sets the dictionary containing the open task alerts.
    /// The key is the name of the user and the value the amount of task alerts that are still open for that user.
    /// </summary>
    public Dictionary<string, int> OpenTaskAlerts { get; set; }
}