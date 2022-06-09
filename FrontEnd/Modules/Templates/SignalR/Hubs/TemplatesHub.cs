using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Models;
using Microsoft.AspNetCore.SignalR;

namespace FrontEnd.Modules.Templates.SignalR.Hubs;

public class TemplatesHub : Hub
{
    private readonly IBaseService baseService;

    private static ConcurrentDictionary<string, List<ConnectedUserModel>> connectedUsers;

    public TemplatesHub(IBaseService baseService)
    {
        this.baseService = baseService;

        connectedUsers ??= new ConcurrentDictionary<string, List<ConnectedUserModel>>();
    }

    /// <summary>
    /// Creates the prefix used for keys in the <see cref="connectedUsers"/> dictionary.
    /// </summary>
    /// <returns>The key prefix.</returns>
    private string GetKeyPrefix()
    {
        return baseService.GetSubDomain();
    }

    private string GetUsersGroupName()
    {
        return $"SignalR_Templates_{baseService.GetSubDomain()}";
    }

    /// <summary>
    /// Retrieves all usernames connected to a specific template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    public IEnumerable<ConnectedUserModel> GetUsersInTemplate(int templateId)
    {
        var users = connectedUsers.TryGetValue($"{GetKeyPrefix()}_{templateId}", out var usersList) ? usersList : null;
        return users == null ? Array.Empty<ConnectedUserModel>() : users.ToArray();
    }

    /// <summary>
    /// Adds a user to the list of connected users for a specific template ID.
    /// </summary>
    /// <param name="templateId">The template ID the user opened.</param>
    /// <param name="username">The username of the user.</param>
    public async Task AddUserAsync(int templateId, string username)
    {
        var key = $"{GetKeyPrefix()}_{templateId}";

        if (!connectedUsers.ContainsKey(key))
        {
            connectedUsers.TryAdd(key, new List<ConnectedUserModel>());
        }

        if (connectedUsers.TryGetValue(key, out var usersList) && usersList.All(u => u.ConnectionId != Context.ConnectionId))
        {
            usersList.Add(new ConnectedUserModel
            {
                ConnectionId = Context.ConnectionId,
                Username = username
            });
        }

        // Notify all users that a user has opened a template.
        await Clients.Group(GetUsersGroupName()).SendAsync("UserOpenedTemplate", templateId);
    }

    /// <summary>
    /// Removes a user from the list of connected users for a specific template ID.
    /// </summary>
    /// <param name="templateId">The template ID the user closed.</param>
    /// <param name="username">The username of the user.</param>
    public async Task RemoveUserAsync(int templateId, string username)
    {
        var key = $"{GetKeyPrefix()}_{templateId}";

        // Check if the template ID is even in the dictionary.
        if (!connectedUsers.ContainsKey(key))
        {
            return;
        }

        // Attempt to remove the user from the list.
        if (!connectedUsers.TryGetValue(key, out var usersList))
        {
            return;
        }

        var index = usersList.FindIndex(u => u.ConnectionId == Context.ConnectionId);
        if (index == -1)
        {
            return;
        }

        usersList.RemoveAt(index);

        // Notify connected users that a user has closed a template.
        await Clients.Group(GetUsersGroupName()).SendAsync("UserClosedTemplate", templateId);
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUsersGroupName());
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var groupName = GetUsersGroupName();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        var templateIds = RemoveUserByConnectionId(Context.ConnectionId).ToList();

        // Notify other users that a user has closed a template. Which template ID is unknown at this point, so just send -1 to inform all users should update their current list.
        if (templateIds.Count == 0)
        {
            await Clients.Group(groupName).SendAsync("UserClosedTemplate", -1);
        }
        else
        {
            foreach (var templateId in templateIds)
            {
                await Clients.Group(groupName).SendAsync("UserClosedTemplate", templateId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static IEnumerable<int> RemoveUserByConnectionId(string connectionId)
    {
        var templateIds = new List<int>();

        foreach (var v in connectedUsers)
        {
            var index = v.Value.FindIndex(u => u.ConnectionId == connectionId);
            while (index >= 0)
            {
                var templateId = Int32.Parse(v.Key[(v.Key.LastIndexOf('_') + 1)..]);
                if (templateIds.Contains(templateId))
                {
                    templateIds.Add(templateId);
                }
                
                v.Value.RemoveAt(index);
                index = v.Value.FindIndex(u => u.ConnectionId == connectionId);
            }
        }

        return templateIds;
    }
}