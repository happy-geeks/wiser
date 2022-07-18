using System;

namespace FrontEnd.Modules.Templates.Models;

public class ConnectedUserModel
{
    /// <summary>
    /// Gets or sets the unique connection ID. This is determined by SignalR.
    /// </summary>
    public string ConnectionId { get; init; }

    /// <summary>
    /// Gets or sets the username of the Wiser user.
    /// </summary>
    public string Username { get; set; }
}