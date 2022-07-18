using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services.DataLayer
{
    ///<inheritdoc/>
    public class DynamicContentDataServiceVersionControl : IDynamicContentDataServiceVersionControl
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentDataServiceVersionControl"/>.
        /// </summary>
        public DynamicContentDataServiceVersionControl(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<DynamicContentModel> GetDynamicContentAsync(int contentId, int version)
        {
            var query = $@"SELECT
    id,
    content_id,
    component,
    version
FROM {WiserTableNames.WiserDynamicContent}
WHERE content_id = ?content_id
AND version = ?version";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("content_id", contentId);
            clientDatabaseConnection.AddParameter("version", version);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            var dynamicContentModel = new DynamicContentModel
            {
                Id = Convert.ToInt32(dataTable.Rows[0]["id"]),
                ComponentModeId = Convert.ToInt32(dataTable.Rows[0]["content_id"]),
                Component = dataTable.Rows[0]["component"].ToString(),
                LatestVersion = Convert.ToInt32(dataTable.Rows[0]["version"])
            };

            return dynamicContentModel;
        }

        /// <inheritdoc />
        public async Task CreateNewDynamicContentCommitAsync(DynamicContentCommitModel dynamicContentCommitModel)
        {
            var query = $@"INSERT INTO {WiserTableNames.WiserCommitDynamicContent} (dynamic_content_id, version, commit_id) values (?dynamicContentId, ?version, ?commitId)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentCommitModel.DynamicContentId);
            clientDatabaseConnection.AddParameter("version", dynamicContentCommitModel.Version);
            clientDatabaseConnection.AddParameter("commitId", dynamicContentCommitModel.CommitId);

            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT version, published_environment
FROM {WiserTableNames.WiserDynamicContent}
WHERE content_id = ?dynamicContentId");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<sbyte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentId);

            var baseQueryPart = $@"UPDATE {WiserTableNames.WiserDynamicContent}
SET published_environment = CASE version";

            var dynamicQueryPart = new StringBuilder();
            var dynamicWherePart = new StringBuilder(" AND version IN (");
            foreach (var versionChange in publishModel)
            {
                dynamicQueryPart.Append($" WHEN {versionChange.Key} THEN published_environment + {versionChange.Value}");
                dynamicWherePart.Append($"{versionChange.Key},");
            }
            var endQueryPart = @" END
WHERE content_id = ?dynamicContentId";

            var query = $"{baseQueryPart}{dynamicQueryPart}{endQueryPart}{dynamicWherePart.ToString()[..^1]}";

            clientDatabaseConnection.AddParameter("oldlive", publishLog.OldLive);
            clientDatabaseConnection.AddParameter("oldaccept", publishLog.OldAccept);
            clientDatabaseConnection.AddParameter("oldtest", publishLog.OldTest);
            clientDatabaseConnection.AddParameter("newlive", publishLog.NewLive);
            clientDatabaseConnection.AddParameter("newaccept", publishLog.NewAccept);
            clientDatabaseConnection.AddParameter("newtest", publishLog.NewTest);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);

            var logQuery = $@"INSERT INTO {WiserTableNames.WiserDynamicContentPublishLog} (content_id, old_live, old_accept, old_test, new_live, new_accept, new_test, changed_on, changed_by) 
            VALUES(
                ?dynamicContentId,
                ?oldlive,
                ?oldaccept,
                ?oldtest,
                ?newlive,
                ?newaccept,
                ?newtest,
                ?now,
                ?username
            )";

            return await clientDatabaseConnection.ExecuteAsync($"{query};{logQuery}");
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetDynamicContentWithLowerVersionAsync(int contentId, int version)
        {
            var query = $@"SELECT content_id, version
FROM {WiserTableNames.WiserDynamicContent}
WHERE content_id = ?contentId 
AND version < ?version";

            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("contentId", contentId);
            clientDatabaseConnection.AddParameter("version", version);

            var versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<int>("content_id"));
            }

            return versionList;
        }
    }
}
