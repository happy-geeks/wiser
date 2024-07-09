using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using GeeksCoreLibrary.Core.Middlewares;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Core.Middlewares;

/// <inheritdoc />
public class ApiRequestLoggingMiddleware : RequestLoggingMiddleware
{
    /// <summary>
    /// Overwrite the default table name to save the logs in, to make a log table specifically for Wiser.
    /// </summary>
    protected override string LogTableName => ApiTableNames.ApiRequestLogs;

    /// <inheritdoc />
    public ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IOptions<GclSettings> gclSettings) : base(next, logger, gclSettings)
    {
    }

    /// <inheritdoc />
    protected override Task<ulong> GetUserIdAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var user = (ClaimsIdentity) context.User.Identity;
        return Task.FromResult(IdentityHelpers.GetWiserUserId(user));
    }

    /// <inheritdoc />
    protected override async Task<ulong> LogRequestAsync(HttpContext context, IDatabaseConnection databaseConnection, IServiceProvider serviceProvider)
    {
        // Get the Wiser database connection from the client database connection,
        // so that the logs will be saved in the main Wiser database instead of the tenant database.
        var wiserDatabaseConnection = ((ClientDatabaseConnection)databaseConnection).WiserDatabaseConnection;

        // Call base class method to save generic information from the request.
        var logId = await base.LogRequestAsync(context, wiserDatabaseConnection, serviceProvider);

        return logId;
    }

    /// <inheritdoc />
    protected override async Task LogResponseAsync(ulong logId, HttpContext context, string responseBody, IDatabaseConnection databaseConnection, IServiceProvider serviceProvider)
    {
        if (!GclSettings.RequestLoggingOptions.Enabled || logId == 0)
        {
            return;
        }

        // Get the Wiser database connection from the client database connection,
        // so that the logs will be saved in the main Wiser database instead of the tenant database.
        var wiserDatabaseConnection = ((ClientDatabaseConnection)databaseConnection).WiserDatabaseConnection;

        // Call base class method to save generic information from the response.
        await base.LogResponseAsync(logId, context, responseBody, wiserDatabaseConnection, serviceProvider);

        // Update the log with Wiser specific information.
        try
        {
            var user = (ClaimsIdentity) context.User.Identity;
            var subDomain = IdentityHelpers.GetSubDomain(user);
            var isWiserFrontEndLogin = IdentityHelpers.IsWiserFrontEndLogin(user);

            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("sub_domain", subDomain);
            wiserDatabaseConnection.AddParameter("is_from_wiser_front_end", isWiserFrontEndLogin);
            await wiserDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(LogTableName, logId);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Error while logging request.");
        }
    }
}