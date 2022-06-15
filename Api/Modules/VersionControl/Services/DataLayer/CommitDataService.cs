using System;
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
        public async Task<CreateCommitModel> CreateCommitAsync(CreateCommitModel commitModel)
        {
            //INSERT QUERRY FOR dev_commit
            var query = $@"INSERT INTO dev_commit (description,changedby) VALUES (?description,?changedby)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", commitModel.Description);
            clientDatabaseConnection.AddParameter("changedby", commitModel.ChangedBy);
            var dataTable = await clientDatabaseConnection.ExecuteAsync(query);

            return new CreateCommitModel()
            {
                Description = commitModel.Description,
                AsanaId = commitModel.AsanaId,
                AddedOn = commitModel.AddedOn,
                ChangedBy = commitModel.ChangedBy

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
                $@"SELECT * FROM test.dev_commit ORDER BY addedon desc LIMIT 1;";

            clientDatabaseConnection.ClearParameters();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            int id = Convert.ToInt32(dataTable.Rows[0]["id"]);
            string description = dataTable.Rows[0]["description"].ToString();
            int asanaId = 0;
            string date = dataTable.Rows[0]["addedon"].ToString();
            //DateTime addedOn = DateTime.ParseExact(date, "HH:mm:ss dd.M.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            string changedBy = dataTable.Rows[0]["changedby"].ToString();


            return new CreateCommitModel()
            {
                id = id,
                Description = description,
                AsanaId = asanaId,
                //AddedOn = addedOn,
                ChangedBy = changedBy
            };





        }

    }
}
