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


       

        public Task<bool> CreatePublishLog(int templateId, int version)
        {
            throw new NotImplementedException();
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
