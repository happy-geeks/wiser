using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services.DataLayer;

/// <inheritdoc cref="IVersionControlDataService" />
public class VersionControlDataService : IVersionControlDataService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="VersionControlDataService"/>.
    /// </summary>
    public VersionControlDataService(IDatabaseConnection clientDatabaseConnection)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<int, int>> GetPublishedTemplateIdAndVersionAsync()
    {
        var query = $"""
                     SELECT template_id, version
                     FROM {WiserTableNames.WiserTemplate}
                     WHERE published_environment != 0
                     GROUP BY template_id;
                     """;

        clientDatabaseConnection.ClearParameters();

        var versionList = new Dictionary<int, int>();

        var dataTable = await clientDatabaseConnection.GetAsync(query);

        foreach (DataRow row in dataTable.Rows)
        {
            versionList.Add(row.Field<int>("template_id"), row.Field<int>("version"));
        }

        return versionList;
    }

    /// <inheritdoc />
    public Task<bool> CreatePublishLog(int templateId, int version)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<List<TemplateCommitModel>> GetTemplatesFromCommitAsync(int commitId)
    {
        var query = $"""
                     SELECT
                         commit.commit_id,
                         commit.template_id,
                         commit.version
                     FROM {WiserTableNames.WiserCommitTemplate} AS commit 
                     LEFT JOIN {WiserTableNames.WiserCommitTemplate} AS otherVersion ON otherVersion.template_id = commit.template_id AND otherVersion.version > commit.version
                     WHERE commit.commit_id = ?commitId
                     AND otherVersion.id IS NULL
                     """;

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("commitId", commitId);

        var dataTable = await clientDatabaseConnection.GetAsync(query);

        var templateList = new List<TemplateCommitModel>();

        foreach (DataRow row in dataTable.Rows)
        {
            var template = new TemplateCommitModel
            {
                CommitId = row.Field<int>("commit_id"),
                TemplateId = row.Field<int>("template_id"),
                Version = row.Field<int>("version")
            };

            templateList.Add(template);
        }

        return templateList;
    }

    /// <inheritdoc />
    public async Task<List<DynamicContentCommitModel>> GetDynamicContentFromCommitAsync(int commitId)
    {
        var query = $"""
                     SELECT
                         commit.commit_id,
                         commit.dynamic_content_id,
                         commit.version
                     FROM {WiserTableNames.WiserCommitDynamicContent} AS commit
                     LEFT JOIN {WiserTableNames.WiserCommitDynamicContent} AS otherVersion ON otherVersion.dynamic_content_id = commit.dynamic_content_id AND otherVersion.version > commit.version 
                     WHERE commit.commit_id = ?commitId
                     AND otherVersion.id IS NULL
                     """;

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("commitId", commitId);

        var dataTable = await clientDatabaseConnection.GetAsync(query);

        var dynamicContentList = new List<DynamicContentCommitModel>();

        foreach (DataRow row in dataTable.Rows)
        {
            var dynamicContent = new DynamicContentCommitModel
            {
                CommitId = row.Field<int>("commit_id"),
                DynamicContentId = row.Field<int>("dynamic_content_id"),
                Version = row.Field<int>("version")
            };

            dynamicContentList.Add(dynamicContent);
        }

        return dynamicContentList;
    }
}