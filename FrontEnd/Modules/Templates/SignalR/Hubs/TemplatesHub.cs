using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace FrontEnd.Modules.Templates.SignalR.Hubs;

public class TemplatesHub : Hub
{
    private static ConcurrentDictionary<int, IList<string>> connectedUsers;

    public TemplatesHub()
    {
        connectedUsers ??= new ConcurrentDictionary<int, IList<string>>();
    }

    /// <summary>
    /// Retrieves all users connected to a specific template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    public string[] GetUsersInTemplate(int templateId)
    {
        var users = connectedUsers.TryGetValue(templateId, out var usersList) ? usersList.ToArray() : Array.Empty<string>();
        return users;
    }

    public async Task AddUser(int templateId, string user)
    {
        if (!connectedUsers.ContainsKey(templateId))
        {
            connectedUsers.TryAdd(templateId, new List<string>());
        }

        if (connectedUsers.TryGetValue(templateId, out var usersList) && !usersList.Contains(user))
        {
            usersList.Add(user);
        }

        // Signify all users that the user has opened a template.
        await Clients.All.SendAsync("UserOpenedTemplate", templateId, user);
    }

    public async Task RemoveUser(int templateId, string user)
    {
        // Check if the template ID is even in the dictionary.
        if (!connectedUsers.ContainsKey(templateId))
        {
            return;
        }

        // Attempt to remove the user from the list. If the user is not in the list, the Remove function will return false.
        if (!connectedUsers.TryGetValue(templateId, out var usersList) || !usersList.Remove(user))
        {
            return;
        }

        // Signify connected users that a user has closed a template.
        await Clients.All.SendAsync("UserClosedTemplate", templateId, user);
    }
}