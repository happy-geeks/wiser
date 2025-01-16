using System;
using System.Collections.Generic;
using System.Security.Claims;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;

namespace Api.Core.Services;

/// <inheritdoc cref="IApiReplacementsService" />
public class ApiReplacementsService : IApiReplacementsService, IScopedService
{
    private readonly IStringReplacementsService stringReplacementsService;

    /// <summary>
    /// Creates a new instance of ApiReplacementsService.
    /// </summary>
    public ApiReplacementsService(IStringReplacementsService stringReplacementsService)
    {
        this.stringReplacementsService = stringReplacementsService;
    }

    /// <inheritdoc />
    public string DoIdentityReplacements(string input, ClaimsIdentity identity, bool forQuery = false)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var userId = IdentityHelpers.GetWiserUserId(identity);
        var dictionary = new Dictionary<string, object>
        {
            { "userId", userId },
            { "encryptedUserId", userId.ToString().EncryptWithAesWithSalt(withDateTime: true) },
            { "username", IdentityHelpers.GetUserName(identity, true) },
            { "userType", IdentityHelpers.GetRoles(identity) },
            { "subDomain", IdentityHelpers.GetSubDomain(identity) },
            { "isTest", IdentityHelpers.IsTestEnvironment(identity) }
        };

        return stringReplacementsService.DoReplacements(input, dictionary, forQuery: forQuery);
    }
}