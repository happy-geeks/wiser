using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Enums;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.VersionControl.Services.DataLayer;

/// <inheritdoc cref="ICommitDataService" />
public class CommitDataService : ICommitDataService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="CommitDataService"/>.
    /// </summary>
    public CommitDataService(IDatabaseConnection clientDatabaseConnection)
    {
        databaseConnection = clientDatabaseConnection;
    }

    /// <inheritdoc />
    public async Task<CommitModel> GetCommitAsync(int id)
    {
	    // Get all dynamic content of the commit.
	    var query = $@"SELECT
	`commit`.id,
	`commit`.description,
	`commit`.added_on,
	`commit`.added_by,
	`commit`.completed,
	`commit`.external_id,
	`commit`.deployed_to_development_on,
	`commit`.deployed_to_test_on,
	`commit`.deployed_to_acceptance_on,
	`commit`.deployed_to_live_on,
	`commit`.deployed_to_development_by,
	`commit`.deployed_to_test_by,
	`commit`.deployed_to_acceptance_by,
	`commit`.deployed_to_live_by,
	content.published_environment,
	content.component,
	content.content_id,
	GROUP_CONCAT(DISTINCT template.template_id) AS template_ids,
	GROUP_CONCAT(DISTINCT template.template_name) AS template_names,
	content.title,
	content.version,
	content.changed_on AS content_changed_on,
	content.changed_by AS content_changed_by,
	content.component_mode,
	#contentMaxVersion.version AS max_version,
	IFNULL(testContent.version, 0) AS version_test,
	IFNULL(acceptanceContent.version, 0) AS version_acceptance,
	IFNULL(liveContent.version, 0) AS version_live,
	IFNULL(content.version, 0) <= IFNULL(testContent.version, 0) AS test,
	IFNULL(content.version, 0) <= IFNULL(acceptanceContent.version, 0) AS accept,
	IFNULL(content.version, 0) <= IFNULL(liveContent.version, 0) AS live,
	review.status AS reviewStatus,
	review.reviewed_by AS reviewedBy,
	review.reviewed_by_name AS reviewedByName
FROM {WiserTableNames.WiserCommit} AS `commit`
LEFT JOIN {WiserTableNames.WiserCommitDynamicContent} AS linkToContent ON `commit`.id = linkToContent.commit_id
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS content ON content.content_id = linkToContent.dynamic_content_id AND content.version = linkToContent.version
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS testContent ON testContent.content_id = content.content_id AND (testContent.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS acceptanceContent ON acceptanceContent.content_id = content.content_id AND (acceptanceContent.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS liveContent ON liveContent.content_id = content.content_id AND (liveContent.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserTemplateDynamicContent} AS linkToTemplate ON linkToTemplate.content_id = content.content_id
LEFT JOIN {WiserTableNames.WiserTemplate} AS template ON template.template_id = linkToTemplate.destination_template_id AND template.version = (SELECT MAX(x.version) FROM {WiserTableNames.WiserTemplate} AS x WHERE x.template_id = linkToTemplate.destination_template_id)
LEFT JOIN {WiserTableNames.WiserCommitReviews} AS review ON review.commit_id = `commit`.id
WHERE `commit`.id = ?id
GROUP BY content.content_id";

	    databaseConnection.AddParameter("id", id);
	    var dataTable = await databaseConnection.GetAsync(query);
	    if (dataTable.Rows.Count == 0)
	    {
		    return null;
	    }

	    var commitModel = new CommitModel
	    {
		    Id = id,
		    Description = dataTable.Rows[0].Field<string>("description"),
		    AddedBy = dataTable.Rows[0].Field<string>("added_by"),
		    AddedOn = dataTable.Rows[0].Field<DateTime>("added_on"),
		    ExternalId = dataTable.Rows[0].Field<string>("external_id"),
		    DeployedToDevelopmentOn = dataTable.Rows[0].Field<DateTime?>("deployed_to_development_on"),
		    DeployedToTestOn = dataTable.Rows[0].Field<DateTime?>("deployed_to_test_on"),
		    DeployedToAcceptanceOn = dataTable.Rows[0].Field<DateTime?>("deployed_to_acceptance_on"),
		    DeployedToLiveOn = dataTable.Rows[0].Field<DateTime?>("deployed_to_live_on"),
		    DeployedToDevelopmentBy = dataTable.Rows[0].Field<string>("deployed_to_development_by"),
		    DeployedToTestBy = dataTable.Rows[0].Field<string>("deployed_to_test_by"),
		    DeployedToAcceptanceBy = dataTable.Rows[0].Field<string>("deployed_to_acceptance_by"),
		    DeployedToLiveBy = dataTable.Rows[0].Field<string>("deployed_to_live_by"),
		    Completed = Convert.ToBoolean(dataTable.Rows[0]["completed"]),
		    Review = new ReviewModel
		    {
			    Status =  dataTable.Rows[0].IsNull("reviewStatus") ? ReviewStatuses.None : (ReviewStatuses)Enum.Parse(typeof(ReviewStatuses), dataTable.Rows[0].Field<string>("reviewStatus")),
			    ReviewedBy = dataTable.Rows[0].Field<long?>("reviewedBy") ?? 0,
			    ReviewedByName = dataTable.Rows[0].Field<string>("reviewedByName")
		    }
	    };

	    foreach (DataRow dataRow in dataTable.Rows)
	    {
		    if (dataRow.IsNull("content_id"))
		    {
			    continue;
		    }

		    commitModel.DynamicContents.Add(new DynamicContentCommitModel
		    {
			    DynamicContentId = dataRow.Field<int>("content_id"),
			    Component = dataRow.Field<string>("component"),
			    Title = dataRow.Field<string>("title"),
			    Version = dataRow.Field<int>("version"),
			    ChangedBy = dataRow.Field<string>("content_changed_by"),
			    ChangedOn = dataRow.Field<DateTime>("content_changed_on"),
			    CommitId = id,
			    ComponentMode = dataRow.Field<string>("component_mode"),
			    IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
			    IsLive = Convert.ToBoolean(dataRow["live"]),
			    IsTest = Convert.ToBoolean(dataRow["test"]),
			    VersionAcceptance = dataRow.Field<int>("version_acceptance"),
			    VersionLive = dataRow.Field<int>("version_live"),
			    VersionTest = dataRow.Field<int>("version_test"),
			    TemplateIds = dataRow.Field<string>("template_ids")?.Split(",").Select(Int32.Parse).ToList(),
			    TemplateNames = dataRow.Field<string>("template_names")?.Split(",").ToList()
		    });
	    }

	    // Get all templates of the commit.
	    query = $@"	SELECT
	`commit`.id,
	`commit`.description,
	`commit`.added_on,
	`commit`.added_by,
	`commit`.completed,
	`commit`.external_id,
	`commit`.deployed_to_development_on,
	`commit`.deployed_to_test_on,
	`commit`.deployed_to_acceptance_on,
	`commit`.deployed_to_live_on,
	`commit`.deployed_to_development_by,
	`commit`.deployed_to_test_by,
	`commit`.deployed_to_acceptance_by,
	`commit`.deployed_to_live_by,
	template.template_id,
	template.published_environment,
	template.template_type,
	template.template_name,
	template.version,
	template.parent_id,
	template.changed_by AS template_changed_by,
	template.changed_on AS template_changed_on,
	parent.template_name AS parent_name,
	IFNULL(testTemplate.version, 0) AS version_test,
	IFNULL(acceptanceTemplate.version, 0) AS version_acceptance,
	IFNULL(liveTemplate.version, 0) AS version_live,
	IFNULL(template.version, 0) <= IFNULL(testTemplate.version, 0) AS test,
	IFNULL(template.version, 0) <= IFNULL(acceptanceTemplate.version, 0) AS accept,
	IFNULL(template.version, 0) <= IFNULL(liveTemplate.version, 0) AS live
FROM {WiserTableNames.WiserCommit} AS `commit`
JOIN {WiserTableNames.WiserCommitTemplate} AS linkToTemplate ON `commit`.id = linkToTemplate.commit_id
JOIN {WiserTableNames.WiserTemplate} AS template ON linkToTemplate.template_id = template.template_id AND linkToTemplate.version = template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS testTemplate ON testTemplate.template_id = template.template_id AND (testTemplate.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserTemplate} AS acceptanceTemplate ON acceptanceTemplate.template_id = template.template_id AND (acceptanceTemplate.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserTemplate} AS liveTemplate ON liveTemplate.template_id = template.template_id AND (liveTemplate.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent ON template.parent_id = parent.template_id AND parent.version = (SELECT MAX(x.version) FROM wiser_template AS x WHERE x.template_id = template.parent_id)
WHERE `commit`.id = ?id";

	    dataTable = await databaseConnection.GetAsync(query);
	    foreach (DataRow dataRow in dataTable.Rows)
	    {
		    var publishedEnvironment = (Environments)Convert.ToInt32(dataRow["published_environment"]);

		    commitModel.Templates.Add(new TemplateCommitModel
		    {
			    Version = dataRow.Field<int>("version"),
			    ChangedBy = dataRow.Field<string>("template_changed_by"),
			    ChangedOn = dataRow.Field<DateTime>("template_changed_on"),
			    CommitId = id,
			    IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
			    IsLive = Convert.ToBoolean(dataRow["live"]),
			    IsTest = Convert.ToBoolean(dataRow["test"]),
			    VersionAcceptance = Convert.ToInt32(dataRow["version_acceptance"]),
			    VersionLive = Convert.ToInt32(dataRow["version_live"]),
			    VersionTest = Convert.ToInt32(dataRow["version_test"]),
			    Environment = publishedEnvironment,
			    TemplateId = Convert.ToInt32(dataRow["template_id"]),
			    TemplateName = dataRow.Field<string>("template_name"),
			    TemplateType = (TemplateTypes)Convert.ToInt32(dataRow["template_type"]),
			    TemplateParentId = Convert.ToInt32(dataRow["parent_id"]),
			    TemplateParentName = dataRow.Field<string>("parent_name")
		    });
	    }

	    return commitModel;
    }

    /// <inheritdoc/>
    public async Task<CommitModel> CreateCommitAsync(CommitModel data)
    {
	    databaseConnection.AddParameter("description", data.Description);
	    databaseConnection.AddParameter("externalId", data.ExternalId ?? "");
	    databaseConnection.AddParameter("addedBy", data.AddedBy);
	    databaseConnection.AddParameter("addedOn", data.AddedOn);

	    var query = $@"INSERT INTO {WiserTableNames.WiserCommit} (description, external_id, added_by, added_on) VALUES (?description, ?externalId, ?addedBy, ?addedOn)";
	    data.Id = (int) await databaseConnection.InsertRecordAsync(query);

	    databaseConnection.AddParameter("commitId", data.Id);
	    if (data.Templates != null && data.Templates.Any())
	    {
		    var queryParts = new List<string>();
		    for (var index = 0; index < data.Templates.Count; index++)
		    {
			    var template = data.Templates[index];
			    databaseConnection.AddParameter($"id{index}", template.TemplateId);
			    databaseConnection.AddParameter($"version{index}", template.Version);

			    queryParts.Add($"(?id{index}, ?version{index}, ?commitId, ?addedOn, ?addedBy)");
		    }

		    query = $@"INSERT INTO {WiserTableNames.WiserCommitTemplate} (template_id, version, commit_id, added_on, added_by) VALUES {String.Join(", ", queryParts)}";
		    await databaseConnection.ExecuteAsync(query);
	    }

	    if (data.DynamicContents != null && data.DynamicContents.Any())
	    {
		    var queryParts = new List<string>();
		    for (var index = 0; index < data.DynamicContents.Count; index++)
		    {
			    var dynamicContent = data.DynamicContents[index];
			    databaseConnection.AddParameter($"id{index}", dynamicContent.DynamicContentId);
			    databaseConnection.AddParameter($"version{index}", dynamicContent.Version);

			    queryParts.Add($"(?id{index}, ?version{index}, ?commitId, ?addedOn, ?addedBy)");
		    }

		    query = $@"INSERT INTO {WiserTableNames.WiserCommitDynamicContent} (dynamic_content_id, version, commit_id, added_on, added_by) VALUES {String.Join(", ", queryParts)}";
		    await databaseConnection.ExecuteAsync(query);
	    }

	    return data;
    }

    /// <inheritdoc/>
    public async Task LogDeploymentOfCommitAsync(int id, Environments environment, string username)
    {
	    var queries = new List<string>();
	    if (environment >= Environments.Development)
	    {
		    queries.Add($@"UPDATE {WiserTableNames.WiserCommit} SET deployed_to_development_on = ?deployedOn, deployed_to_development_by = ?username WHERE id = ?id AND deployed_to_development_on IS NULL");
	    }
	    if (environment >= Environments.Test)
	    {
		    queries.Add($@"UPDATE {WiserTableNames.WiserCommit} SET deployed_to_test_on = ?deployedOn, deployed_to_test_by = ?username WHERE id = ?id AND deployed_to_test_on IS NULL");
	    }
	    if (environment >= Environments.Acceptance)
	    {
		    queries.Add($@"UPDATE {WiserTableNames.WiserCommit} SET deployed_to_acceptance_on = ?deployedOn, deployed_to_acceptance_by = ?username WHERE id = ?id AND deployed_to_acceptance_on IS NULL");
	    }
	    if (environment >= Environments.Live)
	    {
		    queries.Add($@"UPDATE {WiserTableNames.WiserCommit} SET deployed_to_live_on = ?deployedOn, deployed_to_live_by = ?username, completed = 1 WHERE id = ?id AND deployed_to_live_on IS NULL");
	    }

	    databaseConnection.AddParameter("id", id);
	    databaseConnection.AddParameter("username", username);
	    databaseConnection.AddParameter("deployedOn", DateTime.Now);
	    databaseConnection.AddParameter("environment", (int)environment);

	    await databaseConnection.ExecuteAsync(String.Join("; ", queries));
    }

    /// <inheritdoc/>
    public async Task<List<TemplateCommitModel>> GetTemplatesToCommitAsync()
    {
        var query = $@"SELECT
	template.template_id,
	template.parent_id,
	template.template_name,
	template.version,
	template.changed_on,
	template.published_environment,
	template.template_type,
	IFNULL(testTemplate.version, 0) AS version_test,
	IFNULL(acceptanceTemplate.version, 0) AS version_acceptance,
	IFNULL(liveTemplate.version, 0) AS version_live,
	template.changed_by,
	parent.template_name AS template_parent,
	IFNULL(template.version, 0) = IFNULL(testTemplate.version, 0) AS test,
	IFNULL(template.version, 0) = IFNULL(acceptanceTemplate.version, 0) AS accept,
	IFNULL(template.version, 0) = IFNULL(liveTemplate.version, 0) AS live 
FROM {WiserTableNames.WiserTemplate} AS template
JOIN {WiserTableNames.WiserTemplate} AS parent ON parent.template_id = template.parent_id AND parent.version = (SELECT MAX(x.version) FROM {WiserTableNames.WiserTemplate} AS x WHERE x.template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS testTemplate ON testTemplate.template_id = template.template_id AND (testTemplate.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserTemplate} AS acceptanceTemplate ON acceptanceTemplate.template_id = template.template_id AND (acceptanceTemplate.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserTemplate} AS liveTemplate ON liveTemplate.template_id = template.template_id AND (liveTemplate.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserCommitTemplate} AS templateCommit ON templateCommit.template_id = template.template_id AND templateCommit.version = template.version
WHERE template.template_type != 7 
AND template.removed = 0
AND otherVersion.id IS NULL
AND templateCommit.id IS NULL
GROUP BY template.template_id
ORDER BY template.changed_on ASC";

	    var results = new List<TemplateCommitModel>();
        var dataTable = await databaseConnection.GetAsync(query);

	    foreach (DataRow dataRow in dataTable.Rows)
        {
	        var item = new TemplateCommitModel
	        {
		        Environment = (Environments) Convert.ToInt32(dataRow["published_environment"]),
		        Version = dataRow.Field<int>("version"),
		        ChangedBy = dataRow.Field<string>("changed_by"),
		        ChangedOn = dataRow.Field<DateTime>("changed_on"),
		        IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
		        IsLive = Convert.ToBoolean(dataRow["live"]),
		        IsTest = Convert.ToBoolean(dataRow["test"]),
		        TemplateId = dataRow.Field<int>("template_id"),
		        TemplateName = dataRow.Field<string>("template_name"),
		        TemplateType = (TemplateTypes) dataRow.Field<int>("template_type"),
		        VersionAcceptance = dataRow.Field<int>("version_acceptance"),
		        VersionLive = dataRow.Field<int>("version_live"),
		        VersionTest = dataRow.Field<int>("version_test"),
		        TemplateParentId = dataRow.Field<int>("parent_id"),
		        TemplateParentName = dataRow.Field<string>("template_parent")
	        };
	        results.Add(item);
        }

	    return results;
    }

    /// <inheritdoc />
    public async Task<List<DynamicContentCommitModel>> GetDynamicContentsToCommitAsync()
    {
        var query = $@"SELECT
	content.content_id,
	content.title,
	content.component,
	content.component_mode,
	content.version,
	content.changed_on,
	content.published_environment,
	content.changed_by,
	IFNULL(testContent.version, 0) AS version_test,
	IFNULL(acceptanceContent.version, 0) AS version_acceptance,
	IFNULL(liveContent.version, 0) AS version_live,
	IFNULL(content.version, 0) = IFNULL(testContent.version, 0) AS test,
	IFNULL(content.version, 0) = IFNULL(acceptanceContent.version, 0) AS accept,
	IFNULL(content.version, 0) = IFNULL(liveContent.version, 0) AS live,
	GROUP_CONCAT(DISTINCT template.template_id) AS template_ids,
	GROUP_CONCAT(DISTINCT template.template_name) AS template_names
FROM {WiserTableNames.WiserDynamicContent} AS content
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = content.content_id AND otherVersion.version > content.version
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS testContent ON testContent.content_id = content.content_id AND (testContent.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS acceptanceContent ON acceptanceContent.content_id = content.content_id AND (acceptanceContent.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS liveContent ON liveContent.content_id = content.content_id AND (liveContent.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserTemplateDynamicContent} AS linkToTemplate ON linkToTemplate.content_id = content.content_id
LEFT JOIN {WiserTableNames.WiserTemplate} AS template ON template.template_id = linkToTemplate.destination_template_id AND template.version = (SELECT MAX(version) FROM wiser_template WHERE template_id = linkToTemplate.destination_template_id)
LEFT JOIN {WiserTableNames.WiserCommitDynamicContent} AS dynamicContentCommit ON dynamicContentCommit.dynamic_content_id = content.content_id AND dynamicContentCommit.version = content.version
WHERE content.removed = 0
AND otherVersion.id IS NULL
AND dynamicContentCommit.id IS NULL
GROUP BY content.content_id
ORDER BY content.changed_on ASC";

	    var results = new List<DynamicContentCommitModel>();
        var dataTable = await databaseConnection.GetAsync(query);

	    foreach (DataRow dataRow in dataTable.Rows)
        {
	        var item = new DynamicContentCommitModel
	        {
		        Version = dataRow.Field<int>("version"),
		        ChangedBy = dataRow.Field<string>("changed_by"),
		        ChangedOn = dataRow.Field<DateTime>("changed_on"),
		        IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
		        IsLive = Convert.ToBoolean(dataRow["live"]),
		        IsTest = Convert.ToBoolean(dataRow["test"]),
		        DynamicContentId = dataRow.Field<int>("content_id"),
		        Title = dataRow.Field<string>("title"),
		        VersionAcceptance = dataRow.Field<int>("version_acceptance"),
		        VersionLive = dataRow.Field<int>("version_live"),
		        VersionTest = dataRow.Field<int>("version_test"),
		        TemplateIds = dataRow.Field<string>("template_ids")?.Split(",").Select(Int32.Parse).ToList(),
		        TemplateNames = dataRow.Field<string>("template_names")?.Split(",").ToList()
	        };
	        results.Add(item);
        }

	    return results;
    }

    /// <inheritdoc />
    public async Task<List<CommitModel>> GetCommitHistoryAsync(bool includeCompleted, bool includeIncompleted)
	{
		if (!includeCompleted && !includeIncompleted)
		{
			throw new ArgumentException("At least one of the parameters includeCompleted or includeIncompleted must be true.");
		}

		var results = new List<CommitModel>();

		var whereClause = "";
		if (includeCompleted && !includeIncompleted)
		{
			whereClause = "WHERE `commit`.completed = TRUE";
		}
		else if (!includeCompleted && includeIncompleted)
		{
			whereClause = "WHERE `commit`.completed = FALSE";
		}

		// Get all commits with dynamic content.
	    var query = $@"SELECT
	`commit`.id,
	`commit`.description,
	`commit`.added_on,
	`commit`.added_by,
	`commit`.completed,
	`commit`.external_id,
	`commit`.deployed_to_development_on,
	`commit`.deployed_to_test_on,
	`commit`.deployed_to_acceptance_on,
	`commit`.deployed_to_live_on,
	`commit`.deployed_to_development_by,
	`commit`.deployed_to_test_by,
	`commit`.deployed_to_acceptance_by,
	`commit`.deployed_to_live_by,
	content.published_environment,
	content.component,
	content.content_id,
	GROUP_CONCAT(DISTINCT template.template_id) AS template_ids,
	GROUP_CONCAT(DISTINCT template.template_name) AS template_names,
	content.title,
	content.version,
	content.changed_on AS content_changed_on,
	content.changed_by AS content_changed_by,
	content.component_mode,
	IFNULL(testContent.version, 0) AS version_test,
	IFNULL(acceptanceContent.version, 0) AS version_acceptance,
	IFNULL(liveContent.version, 0) AS version_live,
	IFNULL(content.version, 0) <= IFNULL(testContent.version, 0) AS test,
	IFNULL(content.version, 0) <= IFNULL(acceptanceContent.version, 0) AS accept,
	IFNULL(content.version, 0) <= IFNULL(liveContent.version, 0) AS live,
	review.status AS reviewStatus,
	review.reviewed_by AS reviewedBy,
	review.reviewed_by_name AS reviewedByName
FROM {WiserTableNames.WiserCommit} AS `commit`
JOIN {WiserTableNames.WiserCommitDynamicContent} AS linkToContent ON `commit`.id = linkToContent.commit_id
JOIN {WiserTableNames.WiserDynamicContent} AS content ON content.content_id = linkToContent.dynamic_content_id AND content.version = linkToContent.version
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS testContent ON testContent.content_id = content.content_id AND (testContent.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS acceptanceContent ON acceptanceContent.content_id = content.content_id AND (acceptanceContent.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS liveContent ON liveContent.content_id = content.content_id AND (liveContent.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserTemplateDynamicContent} AS linkToTemplate ON linkToTemplate.content_id = content.content_id
LEFT JOIN {WiserTableNames.WiserTemplate} AS template ON template.template_id = linkToTemplate.destination_template_id AND template.version = (SELECT MAX(x.version) FROM {WiserTableNames.WiserTemplate} AS x WHERE x.template_id = linkToTemplate.destination_template_id)
LEFT JOIN {WiserTableNames.WiserCommitReviews} AS review ON review.commit_id = `commit`.id
{whereClause}
GROUP BY content.content_id";

	    var dataTable = await databaseConnection.GetAsync(query);
	    foreach (DataRow dataRow in dataTable.Rows)
	    {
		    var commitId = dataRow.Field<int>("id");
		    var commitModel = GetOrAddCommitModel(results, commitId, dataRow);

		    commitModel.DynamicContents.Add(new DynamicContentCommitModel
		    {
			    Component = dataRow.Field<string>("component"),
			    Title = dataRow.Field<string>("title"),
			    Version = dataRow.Field<int>("version"),
			    ChangedBy = dataRow.Field<string>("content_changed_by"),
			    ChangedOn = dataRow.Field<DateTime>("content_changed_on"),
			    CommitId = commitId,
			    ComponentMode = dataRow.Field<string>("component_mode"),
			    IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
			    IsLive = Convert.ToBoolean(dataRow["live"]),
			    IsTest = Convert.ToBoolean(dataRow["test"]),
			    VersionAcceptance = dataRow.Field<int>("version_acceptance"),
			    VersionLive = dataRow.Field<int>("version_live"),
			    VersionTest = dataRow.Field<int>("version_test"),
			    TemplateIds = dataRow.Field<string>("template_ids")?.Split(",").Select(Int32.Parse).ToList(),
			    TemplateNames = dataRow.Field<string>("template_names")?.Split(",").ToList()
		    });
	    }

	    // Get all commits with templates.
	    query = $@"	SELECT
	`commit`.id,
	`commit`.description,
	`commit`.added_on,
	`commit`.added_by,
	`commit`.completed,
	`commit`.external_id,
	`commit`.deployed_to_development_on,
	`commit`.deployed_to_test_on,
	`commit`.deployed_to_acceptance_on,
	`commit`.deployed_to_live_on,
	`commit`.deployed_to_development_by,
	`commit`.deployed_to_test_by,
	`commit`.deployed_to_acceptance_by,
	`commit`.deployed_to_live_by,
	template.template_id,
	template.published_environment,
	template.template_type,
	template.template_name,
	template.version,
	template.parent_id,
	template.changed_by AS template_changed_by,
	template.changed_on AS template_changed_on,
	parent.template_name AS parent_name,
	IFNULL(testTemplate.version, 0) AS version_test,
	IFNULL(acceptanceTemplate.version, 0) AS version_acceptance,
	IFNULL(liveTemplate.version, 0) AS version_live,
	IFNULL(template.version, 0) <= IFNULL(testTemplate.version, 0) AS test,
	IFNULL(template.version, 0) <= IFNULL(acceptanceTemplate.version, 0) AS accept,
	IFNULL(template.version, 0) <= IFNULL(liveTemplate.version, 0) AS live,
	review.status AS reviewStatus,
	review.reviewed_by AS reviewedBy,
	review.reviewed_by_name AS reviewedByName
FROM {WiserTableNames.WiserCommit} AS `commit`
JOIN {WiserTableNames.WiserCommitTemplate} AS linkToTemplate ON `commit`.id = linkToTemplate.commit_id
JOIN {WiserTableNames.WiserTemplate} AS template ON linkToTemplate.template_id = template.template_id AND linkToTemplate.version = template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS testTemplate ON testTemplate.template_id = template.template_id AND (testTemplate.published_environment & {(int)Environments.Test}) = {(int)Environments.Test}
LEFT JOIN {WiserTableNames.WiserTemplate} AS acceptanceTemplate ON acceptanceTemplate.template_id = template.template_id AND (acceptanceTemplate.published_environment & {(int)Environments.Acceptance}) = {(int)Environments.Acceptance}
LEFT JOIN {WiserTableNames.WiserTemplate} AS liveTemplate ON liveTemplate.template_id = template.template_id AND (liveTemplate.published_environment & {(int)Environments.Live}) = {(int)Environments.Live}
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent ON template.parent_id = parent.template_id AND parent.version = (SELECT MAX(x.version) FROM wiser_template AS x WHERE x.template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserCommitReviews} AS review ON review.commit_id = `commit`.id
{whereClause}";

	    dataTable = await databaseConnection.GetAsync(query);
	    foreach (DataRow dataRow in dataTable.Rows)
	    {
		    var commitId = dataRow.Field<int>("id");
		    var commitModel = GetOrAddCommitModel(results, commitId, dataRow);

		    var publishedEnvironment = (Environments)Convert.ToInt32(dataRow["published_environment"]);

		    commitModel.Templates.Add(new TemplateCommitModel
		    {
			    Version = dataRow.Field<int>("version"),
			    ChangedBy = dataRow.Field<string>("template_changed_by"),
			    ChangedOn = dataRow.Field<DateTime>("template_changed_on"),
			    CommitId = commitId,
			    IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
			    IsLive = Convert.ToBoolean(dataRow["live"]),
			    IsTest = Convert.ToBoolean(dataRow["test"]),
			    VersionAcceptance = Convert.ToInt32(dataRow["version_acceptance"]),
			    VersionLive = Convert.ToInt32(dataRow["version_live"]),
			    VersionTest = Convert.ToInt32(dataRow["version_test"]),
			    Environment = publishedEnvironment,
			    TemplateId = Convert.ToInt32(dataRow["template_id"]),
			    TemplateName = dataRow.Field<string>("template_name"),
			    TemplateType = (TemplateTypes)Convert.ToInt32(dataRow["template_type"]),
			    TemplateParentId = Convert.ToInt32(dataRow["parent_id"]),
			    TemplateParentName = dataRow.Field<string>("parent_name")
		    });
	    }

	    if (includeCompleted && !includeIncompleted)
	    {
		    results = results.Where(r => r.Templates.Any(t => t.IsLive) || r.DynamicContents.Any(d => d.IsLive)).ToList();
	    }
	    else if (!includeCompleted && includeIncompleted)
	    {
		    results = results.Where(r => r.Templates.Any(t => !t.IsLive) || r.DynamicContents.Any(d => !d.IsLive)).ToList();
	    }

	    return results
		    .OrderByDescending(r => r.Id)
		    .ToList();
    }

    private static CommitModel GetOrAddCommitModel(List<CommitModel> results, int commitId, DataRow dataRow)
    {
	    var commitModel = results.SingleOrDefault(c => c.Id == commitId);
	    if (commitModel == null)
	    {
		    commitModel = new CommitModel
		    {
			    Id = commitId,
			    Description = dataRow.Field<string>("description"),
			    AddedBy = dataRow.Field<string>("added_by"),
			    AddedOn = dataRow.Field<DateTime>("added_on"),
			    ExternalId = dataRow.Field<string>("external_id"),
			    DeployedToDevelopmentOn = dataRow.Field<DateTime?>("deployed_to_development_on"),
			    DeployedToTestOn = dataRow.Field<DateTime?>("deployed_to_test_on"),
			    DeployedToAcceptanceOn = dataRow.Field<DateTime?>("deployed_to_acceptance_on"),
			    DeployedToLiveOn = dataRow.Field<DateTime?>("deployed_to_live_on"),
			    DeployedToDevelopmentBy = dataRow.Field<string>("deployed_to_development_by"),
			    DeployedToTestBy = dataRow.Field<string>("deployed_to_test_by"),
			    DeployedToAcceptanceBy = dataRow.Field<string>("deployed_to_acceptance_by"),
			    DeployedToLiveBy = dataRow.Field<string>("deployed_to_live_by"),
			    Completed = Convert.ToBoolean(dataRow["completed"]),
			    Review = new ReviewModel
			    {
				    Status =  dataRow.IsNull("reviewStatus") ? ReviewStatuses.None : (ReviewStatuses)Enum.Parse(typeof(ReviewStatuses), dataRow.Field<string>("reviewStatus")),
				    ReviewedBy = dataRow.Field<long?>("reviewedBy") ?? 0,
				    ReviewedByName = dataRow.Field<string>("reviewedByName")
			    }
		    };

		    results.Add(commitModel);
	    }

	    return commitModel;
    }
}