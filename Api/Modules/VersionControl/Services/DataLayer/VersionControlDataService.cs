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
using Api.Modules.Kendo.Models;
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
using Api.Modules.Customers.Interfaces;

namespace Api.Modules.VersionControl.Service.DataLayer
{
   
    ///<inheritdoc/>
    public class VersionControlDataService : IVersionControlDataService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserCustomersService wiserCustomersService;
        /// <summary>
        /// Creates a new instance of <see cref="VersionControlDataService"/>.
        /// </summary>
        public VersionControlDataService(IDatabaseConnection clientDatabaseConnection, IWiserCustomersService wiserCustomersService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserCustomersService = wiserCustomersService; 
        }


        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedTemplateIdAndVersionAsync()
        {

            var query = $@"SELECT template_id, version FROM wiser_template where published_environment != 0 group by template_id;";

            clientDatabaseConnection.ClearParameters();

            Dictionary<int, int> versionList = new Dictionary<int, int>();

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

            var query = "SELECT wct.* FROM wiser_commit_template wct LEFT JOIN wiser_commit_template wct2 ON wct2.template_id = wct.template_id AND wct2.version = wct.version WHERE wct.version = (SELECT MAX(version) FROM wiser_commit_template wct3 WHERE wct3.template_id = wct.template_id) AND wct.commit_id = ?commitId";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("commitId", commitId);

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            List<TemplateCommitModel> templateList = new List<TemplateCommitModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                TemplateCommitModel template = new TemplateCommitModel();
                template.CommitId = row.Field<int>("commit_id");
                template.TemplateId = row.Field<int>("template_id");
                template.Version = row.Field<int>("version");

                templateList.Add(template);
            }

            return templateList;

        }

        /// <inheritdoc />
        public async Task<List<ModuleGridSettings>> GetModuleGridSettingsAsync(int moduleId)
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
                moduleGridSettings.GridReadOptions = row.Field<string>("grid_read_options");


                moduleGridDataList.Add(moduleGridSettings);
            }

            return moduleGridDataList;
        }


        /// <inheritdoc />
        public async Task<List<DynamicContentModel>> GetDynamicContentInTemplateAsync(int templateId)
        {
            var query = $@"SELECT * FROM wiser_template_dynamic_content wtdc LEFT JOIN wiser_dynamic_content dc ON  dc.content_id = wtdc.content_id WHERE dc.version = (SELECT MAX(version) FROM wiser_dynamic_content dc2 WHERE dc2.content_id = dc.content_id) AND destination_template_id = ?templateId AND NOT EXISTS(SELECT * FROM wiser_commit_dynamic_content wcdc WHERE wcdc.dynamic_content_id = dc.content_id and wcdc.version = dc.version)      ";
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

        /// <inheritdoc />
        public async Task<List<DynamicContentCommitModel>> GetDynamicContentfromCommitAsync(int commitId)
        {
            var query = @"SELECT wcdc.* 
                        FROM wiser_commit_dynamic_content wcdc 
                        LEFT JOIN wiser_commit_dynamic_content x ON x.dynamic_content_id = wcdc.dynamic_content_id AND x.version = wcdc.version 
                        WHERE wcdc.version = (SELECT MAX(version) FROM wiser_commit_dynamic_content wcdc2 WHERE wcdc2.dynamic_content_id = wcdc.dynamic_content_id) 
                        AND wcdc.commit_id = ?commitId";

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
