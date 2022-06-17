﻿using System;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Service.DataLayer
{

    public class CommitDataService : ICommitDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientDatabaseConnection"></param>
        public CommitDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<CreateCommitModel> CreateCommitAsync(string commitMessage, string username)
        {
            var query = $@"INSERT INTO wiser_commit (description,changed_by) VALUES (?description,?changedby)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", commitMessage);
            clientDatabaseConnection.AddParameter("changedby", username);
            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return new CreateCommitModel()
            {
                Description = commitMessage,
                ChangedBy = username

            };

        }

        /// <inheritdoc />
        public async Task<bool> CreateCommitItemAsync(int templateId, CommitItemModel commitItemModel)
        {
            var query = $@"INSERT INTO dev_commit_item (commitid,itemid,version) VALUES (?commitid,?itemid,?version)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitid", commitItemModel.CommitId);
            clientDatabaseConnection.AddParameter("itemid", commitItemModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", commitItemModel.Version);
            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return true;
        }

        /// <inheritdoc />
        public async Task<CreateCommitModel> GetCommitAsync()
        {
            var query =
                $@"SELECT * FROM wiser_commit ORDER BY added_on desc LIMIT 1;";

            clientDatabaseConnection.ClearParameters();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            int id = Convert.ToInt32(dataTable.Rows[0]["id"]);
            string description = dataTable.Rows[0]["description"].ToString();
            int asanaId = 0;
            string date = dataTable.Rows[0]["added_on"].ToString();
            string changedBy = dataTable.Rows[0]["changed_by"].ToString();

            return new CreateCommitModel()
            {
                id = id,
                Description = description,
                AsanaId = asanaId,
                ChangedBy = changedBy
            };
        }
    }
}