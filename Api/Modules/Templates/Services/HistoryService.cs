using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
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
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly ITemplateDataService templateDataService;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryService"/>.
        /// </summary>
        public HistoryService(IDynamicContentDataService dataService, IHistoryDataService historyDataService, IWiserCustomersService wiserCustomersService, ITemplateDataService templateDataService)
        {
            this.dataService = dataService;
            this.historyDataService = historyDataService;
            this.wiserCustomersService = wiserCustomersService;
            this.templateDataService = templateDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<HistoryVersionModel>>> GetChangesInComponentAsync(int contentId, int pageNumber, int itemsPerPage)
        {
            var historyList = await GetHistoryOfComponent(contentId, pageNumber, itemsPerPage);
            historyList = historyList.OrderByDescending(version => version.Version).ToList();

            for (var i = 0; i + 1 < historyList.Count; i++)
            {
                historyList[i].Changes = GenerateChangeLogFromDataStrings(historyList[i].Component, historyList[i].ComponentMode, historyList[i].RawVersionString, historyList[i + 1].Component, historyList[i + 1].ComponentMode, historyList[i + 1].RawVersionString);
            }

            return new ServiceResult<List<HistoryVersionModel>>(historyList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> RevertChangesAsync(ClaimsIdentity identity, int contentId, List<RevertHistoryModel> changesToRevert)
        {
            var currentVersion = await dataService.GetComponentDataAsync(contentId);

            foreach (var revisedVersion in changesToRevert)
            {
                var OldVersion = await dataService.GetVersionDataAsync(revisedVersion.GetVersionForRevision(), contentId);

                foreach (var revisedProperty in revisedVersion.RevertedProperties)
                {
                    if (currentVersion.Value.ContainsKey(revisedProperty))
                    {
                        currentVersion.Value[revisedProperty] = OldVersion.Value.GetValueOrDefault(revisedProperty);
                    }
                    else
                    {
                        currentVersion.Value.Add(revisedProperty, OldVersion.Value.GetValueOrDefault(revisedProperty));
                    }
                }
            }

            var componentAndMode = await dataService.GetComponentAndModeFromContentIdAsync(contentId);
            await dataService.SaveAsync(contentId, componentAndMode[0], componentAndMode[1], currentVersion.Key, currentVersion.Value, IdentityHelpers.GetUserName(identity, true));
            return new ServiceResult<int>
            {
                StatusCode = HttpStatusCode.NoContent
            };
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
        public async Task<PublishedEnvironmentModel> GetHistoryVersionsOfDynamicContent(int contentId)
        {
            var versionsAndPublished = await historyDataService.GetPublishedEnvironmentsFromDynamicContentAsync(contentId);

            return PublishedEnvironmentHelper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished);
        }

        /// <inheritdoc />
        public async Task<List<TemplateHistoryModel>> GetVersionHistoryFromTemplate(ClaimsIdentity identity, int templateId, Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContent, int pageNumber, int itemsPerPage)
        {
            var encryptionKey = (await wiserCustomersService.GetEncryptionKey(identity, true)).ModelObject;
            var rawTemplateModels = await historyDataService.GetTemplateHistoryAsync(templateId, pageNumber, itemsPerPage);

            if (rawTemplateModels.Count == 0)
            {
                return new List<TemplateHistoryModel>();
            }

            templateDataService.DecryptEditorValueIfEncrypted(encryptionKey, rawTemplateModels[0]);

            var templateHistory = new List<TemplateHistoryModel>();

            for (var i = 0; i + 1 < rawTemplateModels.Count; i++)
            {
                templateDataService.DecryptEditorValueIfEncrypted(encryptionKey, rawTemplateModels[i + 1]);

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
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId, int pageNumber, int itemsPerPage)
        {
            return await historyDataService.GetPublishHistoryFromTemplateAsync(templateId, pageNumber, itemsPerPage);
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

            CheckIfValuesMatchAndSaveChangesToHistoryModel("name", newVersion.Name, oldVersion.Name, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("editorValue", newVersion.EditorValue, oldVersion.EditorValue, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheMinutes", newVersion.CacheMinutes, oldVersion.CacheMinutes, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheLocation", newVersion.CacheLocation, oldVersion.CacheLocation, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cachePerUrl", newVersion.CachePerUrl, oldVersion.CachePerUrl, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheUsingRegex", newVersion.CacheUsingRegex, oldVersion.CacheUsingRegex, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cachePerHostName", newVersion.CachePerHostName, oldVersion.CachePerHostName, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cachePerQueryString", newVersion.CachePerQueryString, oldVersion.CachePerQueryString, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("cacheRegex", newVersion.CacheRegex, oldVersion.CacheRegex, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRequired", newVersion.LoginRequired, oldVersion.LoginRequired, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loginRole", newVersion.LoginRoles == null ? "" : String.Join(",", newVersion.LoginRoles), oldVersion.LoginRoles == null ? "" : String.Join(",", oldVersion.LoginRoles), historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("insertMode", newVersion.InsertMode, oldVersion.InsertMode, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("loadAlways", newVersion.LoadAlways, oldVersion.LoadAlways, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("disableMinifier", newVersion.DisableMinifier, oldVersion.DisableMinifier, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("urlRegex", newVersion.UrlRegex, oldVersion.UrlRegex, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("externalFiles", String.Join(";", newVersion.ExternalFiles ?? new List<string>()), String.Join(";", oldVersion.ExternalFiles ?? new List<string>()), historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("groupingCreateObjectInsteadOfArray", newVersion.GroupingCreateObjectInsteadOfArray, oldVersion.GroupingCreateObjectInsteadOfArray, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("groupingPrefix", newVersion.GroupingPrefix, oldVersion.GroupingPrefix, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("groupingKey", newVersion.GroupingKey, oldVersion.GroupingKey, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("groupingKeyColumnName", newVersion.GroupingKeyColumnName, oldVersion.GroupingKeyColumnName, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("groupingValueColumnName", newVersion.GroupingValueColumnName, oldVersion.GroupingValueColumnName, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("isScssIncludeTemplate", newVersion.IsScssIncludeTemplate, oldVersion.IsScssIncludeTemplate, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("useInWiserHtmlEditors", newVersion.UseInWiserHtmlEditors, oldVersion.UseInWiserHtmlEditors, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("preLoadQuery", newVersion.PreLoadQuery, oldVersion.PreLoadQuery, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("returnNotFoundWhenPreLoadQueryHasNoData", newVersion.ReturnNotFoundWhenPreLoadQueryHasNoData, oldVersion.ReturnNotFoundWhenPreLoadQueryHasNoData, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("routineType", newVersion.RoutineType, oldVersion.RoutineType, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("routineParameters", newVersion.RoutineParameters, oldVersion.RoutineParameters, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("routineReturnType", newVersion.RoutineReturnType, oldVersion.RoutineReturnType, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("triggerTiming", newVersion.TriggerTiming, oldVersion.TriggerTiming, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("triggerEvent", newVersion.TriggerEvent, oldVersion.TriggerEvent, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("triggerTableName", newVersion.TriggerTableName, oldVersion.TriggerTableName, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("isDefaultHeader", newVersion.IsDefaultHeader, oldVersion.IsDefaultHeader, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("isDefaultFooter", newVersion.IsDefaultFooter, oldVersion.IsDefaultFooter, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("defaultHeaderFooterRegex", newVersion.DefaultHeaderFooterRegex, oldVersion.DefaultHeaderFooterRegex, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("isPartial", newVersion.IsPartial, oldVersion.IsPartial, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("widgetContent", newVersion.WidgetContent, oldVersion.WidgetContent, historyModel);
            CheckIfValuesMatchAndSaveChangesToHistoryModel("widgetLocation", newVersion.WidgetLocation, oldVersion.WidgetLocation, historyModel);

            var oldLinkedTemplates = newVersion.LinkedTemplates.RawLinkList.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var newLinkedTemplates = oldVersion.LinkedTemplates.RawLinkList.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (!String.IsNullOrEmpty(newVersion.LinkedTemplates.RawLinkList))
            {
                foreach (var item in oldLinkedTemplates)
                {
                    if (!newLinkedTemplates.Contains(item))
                    {
                        historyModel.LinkedTemplateChanges.Add(item.Split(";")[1], new KeyValuePair<object, object>(true, false));
                    }
                }
            }
            if (!String.IsNullOrEmpty(oldVersion.LinkedTemplates.RawLinkList))
            {
                foreach (var item in newLinkedTemplates)
                {
                    if (!oldLinkedTemplates.Contains(item))
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
            if (Equals(newValue, oldValue))
            {
                return;
            }

            if ((newValue == null || (newValue is string stringValue && String.IsNullOrWhiteSpace(stringValue)))
                && (oldValue == null || (oldValue is string oldStringValue && String.IsNullOrWhiteSpace(oldStringValue))))
            {
                return;
            }

            templateModel.TemplateChanges.Add(propName, new KeyValuePair<object, object>(newValue, oldValue));
        }

        /// <summary>
        /// Get the raw list of versions of the component. These historymodels have a rawdatastring and no generated changes.
        /// </summary>
        /// <param name="templateId">The id of the content to retrieve the versions of.</param>
        /// <param name="pageNumber">What page number to load</param>
        /// <param name="itemsPerPage">How many versions are being loaded per page</param>
        /// <returns>List of HistoryVersionModels forming</returns>
        private async Task<List<HistoryVersionModel>> GetHistoryOfComponent(int templateId, int pageNumber, int itemsPerPage)
        {
            var olderVersions = await historyDataService.GetDynamicContentHistoryAsync(templateId, pageNumber, itemsPerPage);

            return olderVersions;
        }

        /// <summary>
        /// Generates the changes between 2 versions. This will loop through the versions and will take the settings that have changed to add them to a versions changes.
        /// </summary>
        /// <param name="newMode">The mode of the new version</param>
        /// <param name="newVersion">The raw datastring of the newer version</param>
        /// <param name="oldMode">The mode of the old version</param>
        /// <param name="oldVersion">The raw datastring of the older version</param>
        /// <param name="newComponent">The component of the new version</param>
        /// <param name="oldComponent">the component of the old version</param>
        /// <returns>List of changes that can be added to the changes of the newer versions HistoryVersionModel.</returns>
        private List<DynamicContentChangeModel> GenerateChangeLogFromDataStrings(string newComponent, string newMode, string newVersion, string oldComponent, string oldMode, string oldVersion)
        {
            var newVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(newVersion ?? "{}") ?? new Dictionary<string, JToken>();
            var oldVersionDict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(oldVersion ?? "{}") ?? new Dictionary<string, JToken>();

            var changeLog = new List<DynamicContentChangeModel>();

            foreach (var dataValue in newVersionDict)
            {
                if (oldVersionDict.ContainsKey(dataValue.Key) && !oldVersionDict[dataValue.Key].Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(newComponent, dataValue.Key, dataValue.Value, oldVersionDict.GetValueOrDefault(dataValue.Key), newMode));

                    oldVersionDict.Remove(dataValue.Key);
                }
                else if (!oldVersionDict.ContainsKey(dataValue.Key) && !String.IsNullOrEmpty(dataValue.Value.ToString()))
                {
                    changeLog.Add(new DynamicContentChangeModel(newComponent, dataValue.Key, dataValue.Value, "", newMode));
                }
            }

            //Check if the olderversion contains fields that the newVersion does not
            foreach (var dataValue in oldVersionDict)
            {
                if (newVersionDict.ContainsKey(dataValue.Key) && !newVersionDict[dataValue.Key].Equals(dataValue.Value))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldComponent, dataValue.Key, newVersionDict.GetValueOrDefault(dataValue.Key), dataValue.Value, oldMode));
                }
                else if (!newVersionDict.ContainsKey(dataValue.Key) && !String.IsNullOrEmpty(dataValue.Value.ToString()))
                {
                    changeLog.Add(new DynamicContentChangeModel(oldComponent, dataValue.Key, "", dataValue.Value, oldMode));
                }
            }
            return changeLog;
        }
    }
}