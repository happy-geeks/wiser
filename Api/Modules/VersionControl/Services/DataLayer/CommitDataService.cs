using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services.DataLayer
{
    ///<inheritdoc/>
    public class CommitDataService : ICommitDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="CommitDataService"/>.
        /// </summary>
        public CommitDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<List<TemplateCommitModel>> GetTemplateCommitsAsync()
        {
            /*
             * SELECT
	template.template_id,
	template.parent_id,
	template.template_name,
	template.version,
	template.changed_on,
	template.published_environment,
	template.template_type,
CASE
		
		WHEN test_template.version IS NULL THEN
		0 ELSE test_template.version 
	END AS versie_test,
CASE
		
		WHEN acceptatie_template.version IS NULL THEN
		0 ELSE acceptatie_template.version 
	END AS versie_acceptatie,
CASE
		
		WHEN live_template.version IS NULL THEN
		0 ELSE live_template.version 
	END AS versie_live,
	t.changed_by,
	m.template_name AS template_parent,
CASE
		
		WHEN test_template.version IS NULL THEN
		0 ELSE 1 
	END AS test,
CASE
		
		WHEN acceptatie_template.version IS NULL THEN
		0 ELSE 1 
	END AS accept,
CASE
		
		WHEN live_template.version IS NULL THEN
		0 ELSE 1 
	END AS live 
FROM wiser_template AS template
LEFT JOIN wiser_template m ON t.parent_id = m.template_id
LEFT JOIN wiser_template test_template ON test_template.template_id = t.template_id 
AND (
	test_template.published_environment = 2 
	OR test_template.published_environment = 6 
	OR test_template.published_environment = 10 
	OR test_template.published_environment = 14 
	OR test_template.published_environment = 3 
	OR test_template.published_environment = 7 
	OR test_template.published_environment = 11 
	OR test_template.published_environment = 15 
) 
AND test_template.template_type != 7
LEFT JOIN wiser_template acceptatie_template ON acceptatie_template.template_id = t.template_id 
AND (
	acceptatie_template.published_environment = 4 
	OR acceptatie_template.published_environment = 6 
	OR acceptatie_template.published_environment = 12 
	OR acceptatie_template.published_environment = 14 
	OR acceptatie_template.published_environment = 5 
	OR acceptatie_template.published_environment = 7 
	OR acceptatie_template.published_environment = 13 
	OR acceptatie_template.published_environment = 15 
) 
AND acceptatie_template.template_type != 7
LEFT JOIN wiser_template live_template ON live_template.template_id = t.template_id 
AND (
	live_template.published_environment = 8 
	OR live_template.published_environment = 10 
	OR live_template.published_environment = 12 
	OR live_template.published_environment = 14 
	OR live_template.published_environment = 9 
	OR live_template.published_environment = 11 
	OR live_template.published_environment = 13 
	OR live_template.published_environment = 13 
	OR live_template.published_environment = 15 
) 
AND live_template.template_type != 7
LEFT JOIN wiser_template test ON test.template_id = t.template_id 
AND test.version = t.version 
AND test.published_environment != 0
LEFT JOIN wiser_template x ON x.template_id = t.template_id 
AND x.version = t.version 
WHERE
	t.version = ( SELECT MAX( version ) FROM wiser_template x2 WHERE x2.template_id = t.template_id ) 
	AND t.template_type != 7 
	AND t.removed = 0 
	AND t.published_environment != 14 
	AND t.published_environment != 15 
	AND NOT EXISTS ( SELECT * FROM wiser_commit_template wdt WHERE wdt.template_id = t.template_id AND wdt.version = t.version ) 
	AND t.parent_id != '' { sort}
             */
        }

        /// <inheritdoc />
        public async Task<CreateCommitModel> CreateCommitAsync(string commitMessage, string username)
        {
            var query = $@"INSERT INTO {WiserTableNames.WiserCommit} (description, changed_by, added_on) VALUES (?description, ?changedBy, ?now)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", commitMessage);
            clientDatabaseConnection.AddParameter("changedBy", username);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            var newId = await clientDatabaseConnection.InsertRecordAsync(query);

            return new CreateCommitModel
            {
                Id = (int)newId,
                Description = commitMessage,
                ChangedBy = username
            };
        }
        
        /// <inheritdoc/>
        public async Task CompleteCommitAsync(int commitId, bool commitCompleted)
        {
            var query = $@"UPDATE {WiserTableNames.WiserCommit} SET completed = ?commitCompleted WHERE id = ?commitId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitId", commitId);
            clientDatabaseConnection.AddParameter("commitCompleted", commitCompleted);

            await clientDatabaseConnection.ExecuteAsync(query);
        }
    }
}
