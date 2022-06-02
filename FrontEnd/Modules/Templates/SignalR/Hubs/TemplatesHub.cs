using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FrontEnd.Modules.Templates.SignalR.Hubs;

public class TemplatesHub : Hub
{
    private readonly IBaseService baseService;

    private static ConcurrentDictionary<string, IList<string>> connectedUsers;

    public TemplatesHub(IBaseService baseService)
    {
        this.baseService = baseService;

        connectedUsers ??= new ConcurrentDictionary<string, IList<string>>();
    }

    private string GetKeyPrefix()
    {
        return baseService.GetSubDomain();
    }

    /// <summary>
    /// Retrieves all users connected to a specific template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    public IEnumerable<string> GetUsersInTemplate(int templateId)
    {
        var users = connectedUsers.TryGetValue($"{GetKeyPrefix()}_{templateId}", out var usersList) ? usersList.ToArray() : Array.Empty<string>();
        return users;
    }

    public async Task AddUser(int templateId, string user)
    {
        var key = $"{GetKeyPrefix()}_{templateId}";

        if (!connectedUsers.ContainsKey(key))
        {
            connectedUsers.TryAdd(key, new List<string>());
        }

        if (connectedUsers.TryGetValue(key, out var usersList) && !usersList.Contains(user))
        {
            usersList.Add(user);
        }

        // Notify all users that the user has opened a template.
        await Clients.All.SendAsync("UserOpenedTemplate", templateId, user);
    }

    public async Task RemoveUser(int templateId, string user)
    {
        var key = $"{GetKeyPrefix()}_{templateId}";

        // Check if the template ID is even in the dictionary.
        if (!connectedUsers.ContainsKey(key))
        {
            return;
        }

        // Attempt to remove the user from the list. If the user is not in the list, the Remove function will return false.
        if (!connectedUsers.TryGetValue(key, out var usersList) || !usersList.Remove(user))
        {
            return;
        }

        // Notify connected users that a user has closed a template.
        await Clients.All.SendAsync("UserClosedTemplate", templateId, user);
    }
}