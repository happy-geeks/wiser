using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class HistoryService : IHistoryService, IScopedService
    {
        private readonly IDynamicContentDataService dataService;
        private readonly IHistoryDataService historyService;

        public HistoryService(IDynamicContentDataService dataService, IHistoryDataService historyService)
        {
            this.dataService = dataService;
            this.historyService = historyService;
        }
        /// <summary>
        /// Get the raw list of versions of the component. These historymodels have a rawdatastring and no generated changes.
        /// </summary>
        /// <param name="templateId">The id of the content to retrieve the versions of.</param>
        /// <returns>List of HistoryVersionModels forming</returns>
        private async Task<List<HistoryVersionModel>> GetHistoryOfComponent(int templateId)
        {
            var olderVersions = await historyService.GetDynamicContentHistory(templateId);

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
            var helper = new ReflectionHelper();
            var newCmsSettings = helper.GetCmsSettingsTypeByComponentName(newComponent);
            var oldCmsSettings = helper.GetCmsSettingsTypeByComponentName(oldComponent);

            var newVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(newVersion);
            var oldVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(oldVersion);

            var changeLog = new List<DynamicContentChangeModel>();

            foreach (var dataValue in newVersionDict)
            {
                if (oldVersionDict.ContainsKey(dataValue.Key) && !oldVersionDict.GetValueOrDefault(dataValue.Key).Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(newCmsSettings.GetProperty(dataValue.Key), dataValue.Value, oldVersionDict.GetValueOrDefault(dataValue.Key)));

                    oldVersionDict.Remove(dataValue.Key);
                }
                else if (!oldVersionDict.ContainsKey(dataValue.Key)) {
                    changeLog.Add(new DynamicContentChangeModel(newCmsSettings.GetProperty(dataValue.Key), dataValue.Value, ""));
                }
            }

            //Check if the olderversion contains fields that the newVersion does not
            foreach (var dataValue in oldVersionDict)
            {
                if (newVersionDict.ContainsKey(dataValue.Key) && !newVersionDict.GetValueOrDefault(dataValue.Key).Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldCmsSettings.GetProperty(dataValue.Key), newVersionDict.GetValueOrDefault(dataValue.Key), dataValue.Value));
                }
                else if (!newVersionDict.ContainsKey(dataValue.Key))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldCmsSettings.GetProperty(dataValue.Key), "", dataValue.Value));
                }
            }
            return changeLog;
        }

        /// <summary>
        /// Retrieve history of the component with generated changes. The versions will be sorted by the HistoryVersion models version(DESC).
        /// </summary>
        /// <param name="templateId">The id of the content</param>
        /// <returns>List of HistoryVersionModels with generated changes. Sorted by descending version.</returns>
        public async Task<List<HistoryVersionModel>> GetChangesInComponent(int templateId)
        {
            var historyList = await GetHistoryOfComponent(templateId);
            historyList.OrderBy(version => version.GetVersion());

            for (var i = 0; i + 1 < historyList.Count; i++)
            {
                historyList[i].SetChanges(GenerateChangeLogFromDataStrings(historyList[i].GetComponent(), historyList[i].GetComponentMode(), historyList[i].GetRawString(), historyList[i+1].GetComponent(), historyList[i+1].GetComponentMode(), historyList[i + 1].GetRawString()));
            }

            return historyList;
        }

        /// <summary>
        /// Retrieves the current settings and applies the List of changes that should be reverted.
        /// </summary>
        /// <param name="changesToRevert">Contains the properties and specific versions that need to be reverted.</param>
        /// <returns>An int indicating wether the action was succesfull.</returns>
        public async Task<int> RevertChanges(List<RevertHistoryModel> changesToRevert, int templateId)
        {
            var currentVersion = await dataService.GetTemplateData(templateId);

            foreach (var RevisedVersion in changesToRevert) {
                var OldVersion = await dataService.GetVersionData(RevisedVersion.GetVersionForRevision(), templateId);

                foreach (var RevisedProperty in RevisedVersion.GetRevertedProperties())
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
            var componentAndMode = await dataService.GetComponentAndModeFromContentId(templateId);
            return await dataService.SaveSettingsString(templateId, componentAndMode[0], componentAndMode[1], currentVersion.Key, currentVersion.Value);
        }

        /// <summary>
        /// Retrieve the published environments for dynamic content overviews. This method will accept a list of DynamicContentOverviewModel and retrieve the published environments for each dynamic content.
        /// </summary>
        /// <param name="overviewList">A list of DynamicContentOverviewModels which are to be supplied with published environments</param>
        /// <returns>The list of DynamicContentOverviewModels containing the published environemnts for each model</returns>
        public async Task<List<DynamicContentOverviewModel>> GetPublishedEnvoirementsOfOverviewModels (List<DynamicContentOverviewModel> overviewList)
        {
            var filledOverviews = new List<DynamicContentOverviewModel>();

            foreach (var overview in overviewList)
            {
                overview.versions = await GetHistoryVersionsOfDynamicContent(overview.id);
                filledOverviews.Add(overview);
            }

            return filledOverviews;
        }

        /// <summary>
        /// Retrieves a list of versions for the dynamic content containing their publish status and transforms it to a PublishedEnvironmentModel. 
        /// </summary>
        /// <param name="templateId">The id of the content.</param>
        /// <returns>A PublishedEnvironmentModel containing the published environments of dynamic content</returns>
        public async Task<PublishedEnvironmentModel> GetHistoryVersionsOfDynamicContent(int templateId)
        {
            var versionsAndPublished = await historyService.GetPublishedEnvoirementsFromDynamicContent(templateId);

            var helper = new PublishedEnvironmentHelper();

            return helper.CreatePublishedEnvoirementsFromVersionDictionary(versionsAndPublished);
        }

        /// <summary>
        /// Retrieve the history of a template. This will start by retrieving the history of the template. 
        /// When comparing the setttings for changes the linked dynamic content will be checked for changes during this version. Any changes found in the linked dynamic content will be added to the template history.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="dynamicContent">A Dictionary containing the overview of dynamic content and its respective history</param>
        /// <returns>A list of TemplateHistoryModel containing the history of the template and its linked dynamic content for each version</returns>
        public async Task<List<TemplateHistoryModel>> GetVersionHistoryFromTemplate(int templateId, Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContent)
        {
            var rawTemplateModels = await historyService.GetTemplateHistory(templateId);

            var templateHistory = new List<TemplateHistoryModel>();

            for (var i = 0; i + 1 < rawTemplateModels.Count; i++)
            {
                var historyModel = GenerateHistoryModelForTemplates(rawTemplateModels[i], rawTemplateModels[i + 1]);

                foreach (var historyList in dynamicContent.Values)
                {
                    foreach (var dynamichistory in historyList)
                    {
                        if (dynamichistory.GetChangedOn() > rawTemplateModels[i + 1].changed_on && dynamichistory.GetChangedOn() < rawTemplateModels[i].changed_on)
                        {
                            historyModel.dynamicContentChanges.Add(dynamichistory);
                        }
                    }
                }

                templateHistory.Add(historyModel);
            }
            //Add entry for first version with no changes
            templateHistory.Add(new TemplateHistoryModel(rawTemplateModels[rawTemplateModels.Count-1].templateid, rawTemplateModels[rawTemplateModels.Count-1].version, rawTemplateModels[rawTemplateModels.Count-1].changed_on, rawTemplateModels[rawTemplateModels.Count-1].changed_by));
            return templateHistory;
        }

        /// <summary>
        /// Compares the standardised settings of a template and generate a TemplateHistoryModel containing the changes made between versions. This will also take changes to linked templates into account.
        /// </summary>
        /// <param name="newVersion">A TemplateDataModel of the new version</param>
        /// <param name="oldVersion">A TemplateDataModel of the old version</param>
        /// <returns>A TemplateHistoryModel containing the changes that have been made between the oldversion and the new version</returns>
        private TemplateHistoryModel GenerateHistoryModelForTemplates(TemplateDataModel newVersion, TemplateDataModel oldVersion)
        {
            var historyModel = new TemplateHistoryModel(newVersion.templateid, newVersion.version, newVersion.changed_on, newVersion.changed_by);

            CheckIfValuesMatchAndSaveChangesToHistoryModel("editorValue", newVersion.editorValue, oldVersion.editorValue, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("useCache", newVersion.useCache, oldVersion.useCache, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheMinutes", newVersion.cacheMinutes, oldVersion.cacheMinutes, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleRequests", newVersion.handleRequests, oldVersion.handleRequests, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleSession", newVersion.handleSession, oldVersion.handleSession, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleObjects", newVersion.handleObjects, oldVersion.handleObjects, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleStandards", newVersion.handleStandards, oldVersion.handleStandards, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleTranslations", newVersion.handleTranslations, oldVersion.handleTranslations, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleDynamicContent", newVersion.handleDynamicContent, oldVersion.handleDynamicContent, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleLogicBlocks", newVersion.handleLogicBlocks, oldVersion.handleLogicBlocks, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("handleMutators", newVersion.handleMutators, oldVersion.handleMutators, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRequired", newVersion.loginRequired, oldVersion.loginRequired, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginUserType", newVersion.loginUserType, oldVersion.loginUserType, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginSessionPrefix", newVersion.loginSessionPrefix, oldVersion.loginSessionPrefix, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRole", newVersion.loginRole, oldVersion.loginRole, historyModel);

            var newList = newVersion.linkedTemplates.rawLinkList.Split(",");
            var oldList = oldVersion.linkedTemplates.rawLinkList.Split(",");

            if (!String.IsNullOrEmpty(newVersion.linkedTemplates.rawLinkList)) {
                foreach (var item in newList) {
                    if (!oldList.Contains(item))
                    {
                        historyModel.linkedTemplateChanges.Add(item.Split(";")[1], new KeyValuePair<object, object>(true, false));
                    }
                }
            }
            if (!String.IsNullOrEmpty(oldVersion.linkedTemplates.rawLinkList)) {
                foreach (var item in oldList)
                {
                    if (!newList.Contains(item))
                    {
                        historyModel.linkedTemplateChanges.Add(item.Split(";")[1], new KeyValuePair<object, object>(false, true));
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
                templateModel.templateChanges.Add(propName, new KeyValuePair<object, object>(newValue, oldValue));
            }
        }

        /// <summary>
        /// Retrieves the publish history of a template
        /// </summary>
        /// <param name="templateId">The id of a template</param>
        /// <returns></returns>
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId)
        {
            return await historyService.GetPublishHistoryFromTemplate(templateId);
        }
    }
}
