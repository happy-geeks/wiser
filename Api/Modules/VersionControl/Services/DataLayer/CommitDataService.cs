using System;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
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

            var id = Convert.ToInt32(dataTable.Rows[0]["id"]);
            var description = dataTable.Rows[0]["description"].ToString();
            var asanaId = 0;
            var date = dataTable.Rows[0]["added_on"].ToString();
            var changedBy = dataTable.Rows[0]["changed_by"].ToString();

            return new CreateCommitModel()
            {
                id = id,
                Description = description,
                AsanaId = asanaId,
                ChangedBy = changedBy
            };
        }
        /// <inheritdoc/>
        public async Task<bool> CompleteCommit(int commitId, bool commitCompleted)
        {
            var query = $@"UPDATE wiser_commit SET completed = ?commitCompleted WHERE id = ?commitId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitId", commitId);
            clientDatabaseConnection.AddParameter("commitCompleted", commitCompleted);

            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return true;
        }
    }
}
