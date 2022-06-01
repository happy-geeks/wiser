using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Grids.Models;
using Api.Modules.Modules.Models;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using DocumentFormat.OpenXml.ExtendedProperties;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Newtonsoft.Json;

namespace Api.Modules.VersionControl.Service.DataLayer
{
    /// <summary>
    /// 
    /// </summary>
    public class VersionControlDataService : IVersionControlDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientDatabaseConnection"></param>
        public VersionControlDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        public async Task<CreateCommitModel> CreateCommit(CreateCommitModel commitModel)
        {
            //INSERT QUERRY FOR dev_commit
            var query = $@"INSERT INTO dev_commit (description,changedby) VALUES (?description,?changedby)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", commitModel.Description);
            clientDatabaseConnection.AddParameter("changedby", commitModel.ChangedBy);
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            return new CreateCommitModel()
            {
                Description = commitModel.Description,
                AsanaId = commitModel.AsanaId,
                AddedOn = commitModel.AddedOn,
                ChangedBy = commitModel.ChangedBy

            };

        }

        public async Task<bool> CreateCommitItem(int templateId, CommitItemModel commitItemModel)
        {
            var query = $@"INSERT INTO dev_commit_item (commitid,itemid,version) VALUES (?commitid,?itemid,?version)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitid", commitItemModel.CommitId);
            clientDatabaseConnection.AddParameter("itemid", commitItemModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", commitItemModel.Version);
            var dataTable = await clientDatabaseConnection.GetAsync(query);



            return true;

        }

        public async Task<CreateCommitModel> GetCommit()
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

        public async Task<Dictionary<int, int>> GetPublishedTemplateIdAndVersion()
        {

            var query = $@"SELECT template_id, version FROM test.wiser_template where published_environment != 0 group by template_id;";

            clientDatabaseConnection.ClearParameters();

            Dictionary<int, int> versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("template_id"), row.Field<int>("version"));
            }

            return versionList;
        }

        

        /*
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
                versionList.Add(row.Field<int>("version"), row.Field<int>("template_id") );
            }

            return versionList;
        }*/


        public async Task<bool> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
        {


            var query = $@"INSERT INTO dev_template_live (commitid,itemid,version) VALUES (?commitid,?itemid,?version)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitid", templateCommitModel.CommitId);
            clientDatabaseConnection.AddParameter("itemid", templateCommitModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", templateCommitModel.Version);




            var dataTable = await clientDatabaseConnection.GetAsync(query);



            return true;



        }

        public async Task<bool> UpdateTemplateCommit(TemplateCommitModel templateCommitModel)
        {
            var query = $@"UPDATE dev_template_live SET islive = ?islive, isacceptance = ?isacceptance, istest = ?istest WHERE itemid = ?itemid AND version = ?version";

            clientDatabaseConnection.ClearParameters();
            
            clientDatabaseConnection.AddParameter("islive", templateCommitModel.IsLive);
            clientDatabaseConnection.AddParameter("isacceptance", templateCommitModel.IsAcceptance);
            clientDatabaseConnection.AddParameter("istest", templateCommitModel.IsTest);

            clientDatabaseConnection.AddParameter("itemid", templateCommitModel.TemplateId);
            clientDatabaseConnection.AddParameter("version", templateCommitModel.Version);


            var dataTable = await clientDatabaseConnection.GetAsync(query);



            return true;
        }

        public async Task<bool> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber)
        {
            var query = $@"UPDATE wiser_template SET published_environment = ?publishNumber WHERE id = ?templateId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("publishNumber", publishNumber);
            clientDatabaseConnection.AddParameter("templateId", templateId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            return true;
        }

        public Task<bool> CreatePublishLog(int templateId, int version)
        {
            throw new NotImplementedException();
        }

        public async Task<VersionControlModel> GetCurrentPublishedEnvironment(int templateId, int version)
        {
            var query = "SELECT t.template_id, t.version, t.published_environment FROM wiser_template t WHERE t.template_id = ?templateId AND t.published_environment != 0";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId",templateId);
            clientDatabaseConnection.AddParameter("version", version);

            var dataTable = await clientDatabaseConnection.GetAsync(query);


            if (dataTable.Rows.Count != 0)
            {


                var versionControlModel = new VersionControlModel()
                {
                    TemplateId = Convert.ToInt32(dataTable.Rows[0]["template_id"]),
                    PublishedEnvironments = new PublishedEnvironmentModel()
                    {
                        VersionList = new List<int>()
                    }
                    //Version = Convert.ToInt32(dataTable.Rows[0]["version"]),
                };


                foreach (DataRow template in dataTable.Rows)
                {
                    Console.WriteLine(template["published_environment"]);

                    //PublishedEnvironmentModel publishedEnvironment = new PublishedEnvironmentModel();


                    if (Convert.ToInt32(template["published_environment"]) == 2)
                    {
                        versionControlModel.PublishedEnvironments.TestVersion = 1;
                        versionControlModel.PublishedEnvironments.VersionList.Add(Convert.ToInt32(dataTable.Rows[0]["version"])); 

                    }else if (Convert.ToInt32(template["published_environment"]) == 4)
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

            }else
            {
                return null;
            }

        }

        public async Task<List<TemplateCommitModel>> GetTemplatesFromCommit(int commitId)
        {

            var query = "SELECT dtl.* FROM dev_template_live dtl LEFT JOIN dev_template_live x ON x.itemid = dtl.itemid AND x.version = dtl.version WHERE dtl.version = (SELECT MAX(version) FROM dev_template_live x2 WHERE x2.itemid = dtl.itemid) AND dtl.commitid = ?commitId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitId", commitId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            List<TemplateCommitModel> templateList = new List<TemplateCommitModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                TemplateCommitModel template = new TemplateCommitModel();
                template.CommitId = row.Field<int>("commitid");
                template.TemplateId = row.Field<int>("itemid");
                template.Version = row.Field<int>("version");

                templateList.Add(template);
            }

            return templateList;

        }

        public async Task<List<ModuleGridSettings>> GetModuleGridSettings(int moduleId)
        {
            var query = "SELECT * FROM wiser_module_grids WHERE module_id = ?moduleId";
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("moduleId", moduleId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            List<ModuleGridSettings> moduleGridDataList = new List<ModuleGridSettings>();

            foreach (DataRow row in dataTable.Rows)
            {
                ModuleGridSettings moduleGridSettings = new ModuleGridSettings();
                moduleGridSettings.ModuleId = row.Field<int>("module_id");
                moduleGridSettings.CustomQuery = row.Field<string>("custom_query");
                moduleGridSettings.CountQuery = row.Field<string>("count_query");
                moduleGridSettings.GridOptions = row.Field<string>("grid_options");
                moduleGridSettings.GridDivId = row.Field<string>("grid_div_id");
                moduleGridSettings.Name = row.Field<string>("name");
        

                moduleGridDataList.Add(moduleGridSettings);
            }

            return moduleGridDataList;
        }

        //DYNAMIC CONTENT



        public async Task<DynamicContentModel> GetDynamicContent(int contentId, int version)
        {
            var query = $@"SELECT * FROM wiser_dynamic_content WHERE content_id = ?content_id AND version = ?version";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("content_id", contentId);
            clientDatabaseConnection.AddParameter("version", version);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            var dynamicContentModel = new DynamicContentModel()
            {
                Id = Convert.ToInt32(dataTable.Rows[0]["id"]),
                ComponentModeId = Convert.ToInt32(dataTable.Rows[0]["content_id"]),
                Component = dataTable.Rows[0]["component"].ToString(),
                LatestVersion = Convert.ToInt32(dataTable.Rows[0]["version"])
            };


            return dynamicContentModel;
        }

        public async Task<bool> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel)
        {
            var query = $@"INSERT INTO wiser_commit_dynamic_content (dynamic_content_id,version,commit_id) values (?dynamicContentId, ?version, ?commitId)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentCommitModel.DynamicContentId);
            clientDatabaseConnection.AddParameter("version", dynamicContentCommitModel.Version);
            clientDatabaseConnection.AddParameter("commitId", dynamicContentCommitModel.CommitId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            return true;
        }

        public async Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT wtt.version, wtt.published_environment FROM {WiserTableNames.WiserDynamicContent} wtt WHERE wtt.content_id = ?dynamicContentId");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_environment"));
            }

            return versionList;
        }

        public async Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("dynamicContentId", dynamicContentId);

            var baseQueryPart = $@"UPDATE {WiserTableNames.WiserDynamicContent} wtt 
                SET wtt.published_environment = case wtt.version";

            var dynamicQueryPart = "";
            var dynamicWherePart = " AND wtt.version IN (";
            foreach (var versionChange in publishModel)
            {
                dynamicQueryPart += " WHEN " + versionChange.Key + " THEN wtt.published_environment+" + versionChange.Value;
                dynamicWherePart += versionChange.Key + ",";
            }
            dynamicWherePart = dynamicWherePart.Substring(0, dynamicWherePart.Length - 1) + ")";
            var endQueryPart = @" end
                WHERE wtt.content_id = ?dynamicContentId";

            var query = baseQueryPart + dynamicQueryPart + endQueryPart + dynamicWherePart;

            clientDatabaseConnection.AddParameter("oldlive", publishLog.OldLive);
            clientDatabaseConnection.AddParameter("oldaccept", publishLog.OldAccept);
            clientDatabaseConnection.AddParameter("oldtest", publishLog.OldTest);
            clientDatabaseConnection.AddParameter("newlive", publishLog.NewLive);
            clientDatabaseConnection.AddParameter("newaccept", publishLog.NewAccept);
            clientDatabaseConnection.AddParameter("newtest", publishLog.NewTest);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);

            var logQuery = $@"INSERT INTO wiser_dynamic_content_publish_log (content_id, old_live, old_accept, old_test, new_live, new_accept, new_test, changed_on, changed_by) 
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

            return await clientDatabaseConnection.ExecuteAsync(query + ";" + logQuery);
        }

        public async Task<Dictionary<int, int>> GetDynamicContentWithLowerVersion(int contentId, int version)
        {
            var query = $@"SELECT content_id, version FROM wiser_dynamic_content d where d.content_id = ?contentId AND d.version < ?version AND NOT EXISTS(SELECT * FROM wiser_commit_dynamic_content dt WHERE dt.dynamic_content_id = d.content_id and dt.version = d.version)";

            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("contentId", contentId);
            clientDatabaseConnection.AddParameter("version", version);

            Dictionary<int, int> versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<int>("content_id"));
            }

            return versionList;
        }

        public async Task<List<DynamicContentModel>> GetDynamicContentInTemplate(int templateId)
        {
            var query = $@"SELECT * FROM wiser_template_dynamic_content wtdc LEFT JOIN wiser_dynamic_content dc ON  dc.content_id = wtdc.content_id WHERE dc.version = (SELECT MAX(version) FROM wiser_dynamic_content dc2 WHERE dc2.content_id = dc.content_id) AND destination_template_id = ?templateId";
            clientDatabaseConnection.ClearParameters();

            clientDatabaseConnection.AddParameter("templateId", templateId);
        

            List<DynamicContentModel> dynamicContentModelList = new List<DynamicContentModel>();

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow row in dataTable.Rows)
            {
                dynamicContentModelList.Add(
                    new DynamicContentModel()
                    {
                        Id = row.Field<int>("id"),
                        Version = row.Field<int>("version"),

                    }


                );
            }

            return dynamicContentModelList;
        }

        public async Task<List<DynamicContentCommitModel>> GetDynamicContentfromCommit(int commitId)
        {
            var query = "SELECT wcdc.*\nFROM wiser_commit_dynamic_content wcdc\nLEFT JOIN wiser_commit_dynamic_content x ON x.dynamic_content_id = wcdc.dynamic_content_id AND x.version = wcdc.version       \nWHERE wcdc.version = (SELECT MAX(version) FROM wiser_commit_dynamic_content x2 WHERE x2.dynamic_content_id = wcdc.dynamic_content_id) \nAND wcdc.commit_id = ?commitId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitId", commitId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            List<DynamicContentCommitModel> dynamicContentList = new List<DynamicContentCommitModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                DynamicContentCommitModel DynamicContent = new DynamicContentCommitModel();
                DynamicContent.CommitId = row.Field<int>("commit_id");
                DynamicContent.DynamicContentId = row.Field<int>("dynamic_content_id");
                DynamicContent.Version = row.Field<int>("version");

                dynamicContentList.Add(DynamicContent);
            }

            return dynamicContentList;
        }
    }
}
