using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="IHistoryService" />
    public class HistoryService : IHistoryService, IScopedService
    {
        private readonly IDynamicContentDataService dataService;
        private readonly IHistoryDataService historyDataService;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryService"/>.
        /// </summary>
        public HistoryService(IDynamicContentDataService dataService, IHistoryDataService historyDataService)
        {
            this.dataService = dataService;
            this.historyDataService = historyDataService;
        }

        /// <inheritdoc />
        public async Task<List<HistoryVersionModel>> GetChangesInComponent(int contentId)
        {
            var historyList = await GetHistoryOfComponent(contentId);
            historyList.OrderBy(version => version.Version);

            for (var i = 0; i + 1 < historyList.Count; i++)
            {
                historyList[i].SetChanges(GenerateChangeLogFromDataStrings(historyList[i].Component, historyList[i].ComponentMode, historyList[i].RawVersionString, historyList[i + 1].Component, historyList[i + 1].ComponentMode, historyList[i + 1].RawVersionString));
            }

            return historyList;
        }

        /// <inheritdoc />
        public async Task<int> RevertChanges(ClaimsIdentity identity, List<RevertHistoryModel> changesToRevert, int contentId)
        {
            var currentVersion = await dataService.GetTemplateData(contentId);

            foreach (var RevisedVersion in changesToRevert)
            {
                var OldVersion = await dataService.GetVersionData(RevisedVersion.GetVersionForRevision(), contentId);

                foreach (var RevisedProperty in RevisedVersion.RevertedProperties)
                {
                    if (currentVersion.Value.ContainsKey(RevisedProperty))
                    {
                        currentVersion.Value[RevisedProperty] = OldVersion.Value.GetValueOrDefault(RevisedProperty);
                    }
                    else
                    {
                        currentVersion.Value.Add(RevisedProperty, OldVersion.Value.GetValueOrDefault(RevisedProperty));
                    }
                }
            }
            var componentAndMode = await dataService.GetComponentAndModeFromContentId(contentId);
            return await dataService.SaveSettingsString(contentId, componentAndMode[0], componentAndMode[1], currentVersion.Key, currentVersion.Value, IdentityHelpers.GetUserName(identity));
        }

        /// <inheritdoc />
        public async Task<List<DynamicContentOverviewModel>> GetPublishedEnvironmentsOfOverviewModels(List<DynamicContentOverviewModel> overviewList)
        {
            foreach (var overview in overviewList)
            {
                overview.Versions = await GetHistoryVersionsOfDynamicContent(overview.Id);
            }

            return overviewList;
        }

        /// <inheritdoc />
        public async Task<PublishedEnvironmentModel> GetHistoryVersionsOfDynamicContent(int templateId)
        {
            var versionsAndPublished = await historyDataService.GetPublishedEnvironmentsFromDynamicContent(templateId);

            var helper = new PublishedEnvironmentHelper();

            return helper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished);
        }
        
        /// <inheritdoc />
        public async Task<List<TemplateHistoryModel>> GetVersionHistoryFromTemplate(int templateId, Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContent)
        {
            var rawTemplateModels = await historyDataService.GetTemplateHistory(templateId);

            var templateHistory = new List<TemplateHistoryModel>();

            for (var i = 0; i + 1 < rawTemplateModels.Count; i++)
            {
                var historyModel = GenerateHistoryModelForTemplates(rawTemplateModels[i], rawTemplateModels[i + 1]);

                foreach (var historyList in dynamicContent.Values)
                {
                    foreach (var dynamichistory in historyList)
                    {
                        if (dynamichistory.ChangedOn > rawTemplateModels[i + 1].ChangedOn && dynamichistory.ChangedOn < rawTemplateModels[i].ChangedOn)
                        {
                            historyModel.DynamicContentChanges.Add(dynamichistory);
                        }
                    }
                }

                templateHistory.Add(historyModel);
            }

            //Add entry for first version with no changes
            templateHistory.Add(new TemplateHistoryModel(rawTemplateModels.Last().TemplateId, rawTemplateModels.Last().Version, rawTemplateModels.Last().ChangedOn, rawTemplateModels.Last().ChangedBy));
            return templateHistory;
        }
        
        /// <inheritdoc />
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId)
        {
            return await historyDataService.GetPublishHistoryFromTemplate(templateId);
        }

        /// <summary>
        /// Compares the standardised settings of a template and generate a TemplateHistoryModel containing the changes made between versions. This will also take changes to linked templates into account.
        /// </summary>
        /// <param name="newVersion">A <see cref="TemplateSettingsModel"/> of the new version</param>
        /// <param name="oldVersion">A <see cref="TemplateSettingsModel"/> of the old version</param>
        /// <returns>A TemplateHistoryModel containing the changes that have been made between the oldversion and the new version</returns>
        private TemplateHistoryModel GenerateHistoryModelForTemplates(TemplateSettingsModel newVersion, TemplateSettingsModel oldVersion)
        {
            var historyModel = new TemplateHistoryModel(newVersion.TemplateId, newVersion.Version, newVersion.ChangedOn, newVersion.ChangedBy);

            CheckIfValuesMatchAndSaveChangesToHistoryModel("editorValue", newVersion.EditorValue, oldVersion.EditorValue, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("useCache", newVersion.UseCache, oldVersion.UseCache, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheMinutes", newVersion.CacheMinutes, oldVersion.CacheMinutes, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleRequests", newVersion.HandleRequests, oldVersion.HandleRequests, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleSession", newVersion.HandleSession, oldVersion.HandleSession, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleObjects", newVersion.HandleObjects, oldVersion.HandleObjects, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleStandards", newVersion.HandleStandards, oldVersion.HandleStandards, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleTranslations", newVersion.HandleTranslations, oldVersion.HandleTranslations, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleDynamicContent", newVersion.HandleDynamicContent, oldVersion.HandleDynamicContent, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleLogicBlocks", newVersion.HandleLogicBlocks, oldVersion.HandleLogicBlocks, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleMutators", newVersion.HandleMutators, oldVersion.HandleMutators, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRequired", newVersion.LoginRequired, oldVersion.LoginRequired, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginUserType", newVersion.LoginUserType, oldVersion.LoginUserType, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginSessionPrefix", newVersion.LoginSessionPrefix, oldVersion.LoginSessionPrefix, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRole", newVersion.LoginRole, oldVersion.LoginRole, historyModel);

            var newList = newVersion.LinkedTemplates.RawLinkList.Split(",");
            var oldList = oldVersion.LinkedTemplates.RawLinkList.Split(",");

            if (!String.IsNullOrEmpty(newVersion.LinkedTemplates.RawLinkList))
            {
                foreach (var item in newList)
                {
                    if (!oldList.Contains(item))
                    {
                        historyModel.LinkedTemplateChanges.Add(item.Split(";")[1], new KeyValuePair<object, object>(true, false));
                    }
                }
            }
            if (!String.IsNullOrEmpty(oldVersion.LinkedTemplates.RawLinkList))
            {
                foreach (var item in oldList)
                {
                    if (!newList.Contains(item))
                    {
                        historyModel.LinkedTemplateChanges.Add(item.Split(";")[1], new KeyValuePair<object, object>(false, true));
                    }
                }
            }

            return historyModel;
        }

        /// <summary>
        /// Compares 2 values of a property and saves differences to a TemplateHistoryModel.
        /// </summary>
        /// <param name="propName">The name of the property that is compared</param>
        /// <param name="newValue">The new value of the property</param>
        /// <param name="oldValue">The old value of the property</param>
        /// <param name="templateModel">The TemplateHistoryModel to which differences will be saved</param>
        private void CheckIfValuesMatchAndSaveChangesToHistoryModel(string propName, object newValue, object oldValue, TemplateHistoryModel templateModel)
        {
            if (!Equals(newValue, oldValue))
            {
                templateModel.TemplateChanges.Add(propName, new KeyValuePair<object, object>(newValue, oldValue));
            }
        }

        /// <summary>
        /// Get the raw list of versions of the component. These historymodels have a rawdatastring and no generated changes.
        /// </summary>
        /// <param name="templateId">The id of the content to retrieve the versions of.</param>
        /// <returns>List of HistoryVersionModels forming</returns>
        private async Task<List<HistoryVersionModel>> GetHistoryOfComponent(int templateId)
        {
            var olderVersions = await historyDataService.GetDynamicContentHistory(templateId);

            return olderVersions;
        }

        /// <summary>
        /// Generates the changes between 2 versions. This will loop through the vesrions and will take the settings that have changed to add them to a versions changes.
        /// </summary>
        /// <param name="newVersion">The raw datastring of the newer version</param>
        /// <param name="oldVersion">The raw datastring of the older version</param>
        /// <returns>List of changes that can be added to the changes of the newer versions HistoryVersionModel.</returns>
        private List<DynamicContentChangeModel> GenerateChangeLogFromDataStrings(string newComponent, string newMode, string newVersion, string oldComponent, string oldMode, string oldVersion)
        {
            var newVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(newVersion);
            var oldVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(oldVersion);

            var changeLog = new List<DynamicContentChangeModel>();

            foreach (var dataValue in newVersionDict)
            {
                if (oldVersionDict.ContainsKey(dataValue.Key) && !oldVersionDict.GetValueOrDefault(dataValue.Key).Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(newComponent, dataValue.Key, dataValue.Value, oldVersionDict.GetValueOrDefault(dataValue.Key)));

                    oldVersionDict.Remove(dataValue.Key);
                }
                else if (!oldVersionDict.ContainsKey(dataValue.Key))
                {
                    changeLog.Add(new DynamicContentChangeModel(newComponent, dataValue.Key, dataValue.Value, ""));
                }
            }

            //Check if the olderversion contains fields that the newVersion does not
            foreach (var dataValue in oldVersionDict)
            {
                if (newVersionDict.ContainsKey(dataValue.Key) && !newVersionDict.GetValueOrDefault(dataValue.Key).Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldComponent, dataValue.Key, newVersionDict.GetValueOrDefault(dataValue.Key), dataValue.Value));
                }
                else if (!newVersionDict.ContainsKey(dataValue.Key))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldComponent, dataValue.Key, "", dataValue.Value));
                }
            }
            return changeLog;
        }
    }
}
