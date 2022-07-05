using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services.DataLayer
{
    ///<inheritdoc/>
    public class TemplateContainerDataService : ITemplateContainerDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        /// <summary>
        /// Creates a new instance of <see cref="TemplateContainerDataService"/>.
        /// </summary>
        public TemplateContainerDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetTemplatesWithLowerVersionAsync(int templateId, int version)
        {
            var query = $@"SELECT template_id, version FROM wiser_template t where t.template_id = ?templateId AND t.version < ?version AND NOT EXISTS(SELECT * FROM wiser_commit_template dt WHERE dt.template_id = t.template_id and dt.version = t.version)  ";

            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("version", version);

            var versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<int>("template_id"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<TemplateEnvironments> GetCurrentPublishedEnvironmentAsync(int templateId, int version)
        {
            var query = "SELECT t.template_id, t.version, t.published_environment FROM wiser_template t WHERE t.template_id = ?templateId AND t.published_environment != 0";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("version", version);

            var dataTable = await clientDatabaseConnection.GetAsync(query);


            if (dataTable.Rows.Count != 0)
            {
                var versionControlModel = new TemplateEnvironments()
                {
                    TemplateId = Convert.ToInt32(dataTable.Rows[0]["template_id"]),
                    PublishedEnvironments = new PublishedEnvironmentModel()
                    {
                        VersionList = new List<int>()
                    }
                };


                foreach (DataRow template in dataTable.Rows)
                {
                    if (Convert.ToInt32(template["published_environment"]) == 2)
                    {
                        versionControlModel.PublishedEnvironments.TestVersion = 1;
                        versionControlModel.PublishedEnvironments.VersionList.Add(Convert.ToInt32(dataTable.Rows[0]["version"]));
                    }
                    else if (Convert.ToInt32(template["published_environment"]) == 4)
                    {
                        versionControlModel.PublishedEnvironments.AcceptVersion = 1;
                        versionControlModel.PublishedEnvironments.VersionList.Add(Convert.ToInt32(dataTable.Rows[0]["version"]));
                    }
                    else if (Convert.ToInt32(template["published_environment"]) == 8)
                    {
                        versionControlModel.PublishedEnvironments.AcceptVersion = 1;
                        versionControlModel.PublishedEnvironments.VersionList.Add(Convert.ToInt32(dataTable.Rows[0]["version"]));
                    }
                }
                return versionControlModel;
            }
            else
            {
                return null;
            }

        }

        /// <inheritdoc />
        public async Task<bool> CreateNewTemplateCommitAsync(TemplateCommitModel templateCommitModel)
        {
            var query = $@"INSERT INTO wiser_commit_template (commit_id,template_id,version) VALUES (?commitid,?itemid,?version)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitid", templateCommitModel.CommitId);
            clientDatabaseConnection.AddParameter("itemid", templateCommitModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", templateCommitModel.Version);

            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateTemplateCommitAsync(TemplateCommitModel templateCommitModel)
        {
            var query = $@"UPDATE wiser_commit_template SET islive = ?islive, isacceptance = ?isacceptance, istest = ?istest WHERE template_id = ?itemid AND version = ?version";

            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("islive", templateCommitModel.IsLive);
            clientDatabaseConnection.AddParameter("isacceptance", templateCommitModel.IsAcceptance);
            clientDatabaseConnection.AddParameter("istest", templateCommitModel.IsTest);

            clientDatabaseConnection.AddParameter("itemid", templateCommitModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", templateCommitModel.Version);

            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdatePublishEnvironmentTemplateAsync(int templateId, int publishNumber)
        {
            var query = $@"UPDATE wiser_template SET published_environment = ?publishNumber WHERE id = ?templateId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("publishNumber", publishNumber);
            clientDatabaseConnection.AddParameter("templateId", templateId);

            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return true;
        }
    }
}
