using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Service.DataLayer
{
    public class TemplateContainerDataService : ITemplateContainerDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientDatabaseConnection"></param>
        public TemplateContainerDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }


        public async Task<Dictionary<int, int>> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            var query = $@"SELECT template_id, version FROM wiser_template t where t.template_id = ?templateId AND t.version < ?version AND NOT EXISTS(SELECT * FROM dev_template_live dt WHERE dt.itemid = t.template_id and dt.version = t.version)  ";

            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("version", version);

            Dictionary<int, int> versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<int>("template_id"));
            }

            return versionList;
        }
    }
}
