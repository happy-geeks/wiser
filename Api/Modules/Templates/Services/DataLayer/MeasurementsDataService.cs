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
        string languageCode = null, int pageSize = 500, int pageNumber = 1,
        bool getDailyAverage = false, DateTime? start = null, DateTime? end = null)
    {
        if (templateId <= 0 && componentId <= 0)
        {
            throw new ArgumentException("Please enter a templateId or componentId.");
        }

        var idColumn = componentId > 0 ? "content_id" : "template_id";
        var tableName = componentId > 0 ? WiserTableNames.WiserDynamicContentRenderLog : WiserTableNames.WiserTemplateRenderLog;

        // First build the query with filters and paging.
        clientDatabaseConnection.AddParameter("id", componentId > 0 ? componentId : templateId);

        var whereClause = new List<string>();
        if (environment.HasValue)
        {
            clientDatabaseConnection.AddParameter("environment", environment.ToString());
            whereClause.Add("log.environment = ?environment");
        }

        if (version > 0)
        {
            clientDatabaseConnection.AddParameter("version", version);
            whereClause.Add("log.version = ?version");
        }

        if (!String.IsNullOrWhiteSpace(urlRegex))
        {
            clientDatabaseConnection.AddParameter("urlRegex", urlRegex);
            whereClause.Add("log.url REGEXP ?urlRegex");
        }

        if (userId > 0)
        {
            clientDatabaseConnection.AddParameter("userId", userId);
            whereClause.Add("log.user_id = ?userId");
        }

        if (!String.IsNullOrWhiteSpace(languageCode))
        {
            clientDatabaseConnection.AddParameter("languageCode", languageCode);
            whereClause.Add("log.language_code = ?languageCode");
        }

        if (start.HasValue)
        {
            clientDatabaseConnection.AddParameter("start", start.Value);
            whereClause.Add("log.start >= ?start");
        }

        if (end.HasValue)
        {
            clientDatabaseConnection.AddParameter("end", end.Value);
            whereClause.Add("log.start <= ?end");
        }

        var whereClauseString = "";
        if (whereClause.Any())
        {
            whereClauseString = $"AND {String.Join(" AND ", whereClause)}";
        }

        var query = new StringBuilder(@$"(
    SELECT 
        log.id AS logId,
        log.{idColumn} AS id,
        log.version,
        log.url,
        log.environment,
        log.start, 
        log.end,
        {(getDailyAverage ? "DATE(log.start) AS `date`" : "log.start AS date")},
        {(getDailyAverage ? "AVG(log.time_taken) AS time_taken" : "log.time_taken")},
        log.user_id,
        log.language_code,
        log.error,
        'Template' AS name
    FROM {tableName} AS log
    WHERE log.{idColumn} = ?id
    {whereClauseString}
    {(getDailyAverage ? "GROUP BY DATE(log.start)" : "")}
)");
        if (componentId == 0)
        {
            query.AppendLine(@$"
UNION ALL
(
    SELECT 
        log.id AS logId,
        log.content_id AS id,
        log.version,
        log.url,
        log.environment,
        log.start, 
        log.end,
        {(getDailyAverage ? "DATE(log.start) AS `date`" : "log.start AS date")},
        {(getDailyAverage ? "AVG(log.time_taken) AS time_taken" : "log.time_taken")},
        log.user_id,
        log.language_code,
        log.error,
        component.title AS name
    FROM {WiserTableNames.WiserTemplateDynamicContent} AS link
    JOIN {WiserTableNames.WiserDynamicContent} AS component ON component.content_id = link.content_id AND component.version = (SELECT MAX(x.version) FROM {WiserTableNames.WiserDynamicContent} AS x WHERE x.content_id = link.destination_template_id)
    JOIN {WiserTableNames.WiserDynamicContentRenderLog} AS log ON log.content_id = link.content_id
    {whereClauseString}
    WHERE link.destination_template_id = ?id
    {(getDailyAverage ? "GROUP BY log.content_id, DATE(log.start)" : "")}
)");
        }

        query.AppendLine("ORDER BY start DESC");

        if (pageSize > 0)
        {
            clientDatabaseConnection.AddParameter("take", pageSize);
            clientDatabaseConnection.AddParameter("skip", pageSize * (pageNumber - 1));
            query.AppendLine("LIMIT ?skip, ?take");
        }

        var dataTable = await clientDatabaseConnection.GetAsync(query.ToString());
        return dataTable.Rows.Cast<DataRow>().Select(dataRow =>
        {
            var timeTaken = Convert.ToUInt64(dataRow["time_taken"]);
            return new RenderLogModel
            {
                Id = dataRow.Field<int>("id"),
                Version = dataRow.Field<int>("version"),
                Url = new Uri(dataRow.Field<string>("url")),
                Environment = (Environments) Enum.Parse(typeof(Environments), dataRow.Field<string>("environment"), true),
                Start = dataTable.Columns.Contains("start") ? dataRow.Field<DateTime>("start") : dataRow.Field<DateTime>("date"),
                End = dataTable.Columns.Contains("end") ? dataRow.Field<DateTime>("end") : null,
                TimeTakenInMilliseconds = timeTaken,
                UserId = dataRow.Field<ulong>("user_id"),
                LanguageCode = dataRow.Field<string>("language_code"),
                Error = dataRow.Field<string>("error") ?? "",
                Name = dataRow.Field<string>("name") ?? ""
            };
        }).ToList();
    }
}