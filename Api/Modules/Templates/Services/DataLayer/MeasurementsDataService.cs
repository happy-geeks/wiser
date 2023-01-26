using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Measurements;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Templates.Services.DataLayer;

/// <inheritdoc cref="IMeasurementsDataService" />
public class MeasurementsDataService : IMeasurementsDataService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="MeasurementsDataService"/>.
    /// </summary>
    public MeasurementsDataService(IDatabaseConnection clientDatabaseConnection)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
    }

    /// <inheritdoc />
    public async Task<List<RenderLogModel>> GetRenderLogsAsync(int templateId = 0, int componentId = 0, int version = 0,
        string urlRegex = null, Environments? environment = null, ulong userId = 0,
        string languageCode = null, int pageSize = 500, int pageNumber = 1)
    {
        if (templateId <= 0 && componentId <= 0)
        {
            throw new ArgumentException("Please enter a templateId or componentId.");
        }

        if (templateId > 0 && componentId > 0)
        {
            throw new ArgumentException("Cannot have both a template ID and a component ID, please decide which you want to get the logs for and leave the other one at 0.");
        }

        var idColumn = templateId > 0 ? "template_id" : "component_id";
        var tableName = templateId > 0 ? WiserTableNames.WiserTemplateRenderLog : WiserTableNames.WiserDynamicContentRenderLog;
        
        // First build the query with filters and paging.
        clientDatabaseConnection.AddParameter("id", templateId > 0 ? templateId : componentId);
        var query = new StringBuilder(@$"SELECT 
    id AS logId,
    {idColumn} AS id,
    version,
    url,
    environment,
    start,
    end,
    time_taken,
    user_id,
    language_code,
    error
FROM {tableName}
WHERE {idColumn} = ?id
");
        if (environment.HasValue)
        {
            clientDatabaseConnection.AddParameter("environment", environment.ToString());
            query.AppendLine("AND environment = ?environment");
        }

        if (version > 0)
        {
            clientDatabaseConnection.AddParameter("version", version);
            query.AppendLine("AND version = ?version");
        }

        if (!String.IsNullOrWhiteSpace(urlRegex))
        {
            clientDatabaseConnection.AddParameter("urlRegex", urlRegex);
            query.AppendLine("AND url REGEXP ?urlRegex");
        }

        if (userId > 0)
        {
            clientDatabaseConnection.AddParameter("userId", userId);
            query.AppendLine("AND user_id = ?userId");
        }

        if (!String.IsNullOrWhiteSpace(languageCode))
        {
            clientDatabaseConnection.AddParameter("languageCode", languageCode);
            query.AppendLine("AND language_code = ?languageCode");
        }

        query.AppendLine("ORDER BY logId DESC");
        if (pageSize > 0)
        {
            clientDatabaseConnection.AddParameter("take", pageSize);
            clientDatabaseConnection.AddParameter("skip", pageSize * (pageNumber - 1));
            query.AppendLine("LIMIT ?skip, ?take");
        }

        var dataTable = await clientDatabaseConnection.GetAsync(query.ToString());
        return dataTable.Rows.Cast<DataRow>().Select(dataRow => new RenderLogModel
        {
            Id = dataRow.Field<int>("id"),
            Version = dataRow.Field<int>("version"),
            Url = new Uri(dataRow.Field<string>("url")),
            Environment = (Environments)Enum.Parse(typeof(Environments), dataRow.Field<string>("environment"), true),
            Start = dataRow.Field<DateTime>("start"),
            End = dataRow.Field<DateTime>("end"),
            TimeTaken = TimeSpan.FromMilliseconds(dataRow.Field<ulong>("time_taken")),
            UserId = dataRow.Field<ulong>("user_id"),
            LanguageCode = dataRow.Field<string>("language_code"),
            Error = dataRow.Field<string>("error")
        }).ToList();
    }
}