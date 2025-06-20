﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Enums;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.EntityProperties.Helpers;
using Api.Modules.EntityProperties.Models;
using Api.Modules.Imports.Interfaces;
using Api.Modules.Imports.Models;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.Imports.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Imports.Services;

/// <inheritdoc cref="Api.Modules.Imports.Interfaces.IImportsService" />
public class ImportsService(IWiserItemsService wiserItemsService, IUsersService usersService, IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IExcelService excelService, ILogger<ImportsService> logger) : IImportsService, IScopedService
{
    private const uint ImportLimit = 1000000;

    /// <inheritdoc />
    public async Task<ServiceResult<ImportResultModel>> PrepareImportAsync(ClaimsIdentity identity, ImportRequestModel importRequest)
    {
        if (String.IsNullOrWhiteSpace(importRequest.FilePath) || !File.Exists(importRequest.FilePath))
        {
            return new ServiceResult<ImportResultModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "File path is either empty, or the file does not exist."
            };
        }

        // If the user entered an email address, we need to change it for the user.
        var subDomain = IdentityHelpers.GetSubDomain(identity);
        var userId = IdentityHelpers.GetWiserUserId(identity);
        if (!String.IsNullOrWhiteSpace(importRequest.EmailAddress))
        {
            await usersService.ChangeEmailAddressAsync(userId, subDomain, importRequest.EmailAddress, identity);
        }

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
        var entityType = importRequest.ImportSettings.Count > 0 ? Convert.ToString(importRequest.ImportSettings.First()["entityType"]) : "";
        var moduleId = importRequest.ImportSettings.Count > 0 ? Convert.ToInt32(importRequest.ImportSettings.First()["moduleId"]) : 0;
        var importResult = new ImportResultModel();
        var fileBytes = await File.ReadAllBytesAsync(importRequest.FilePath);

        // If the file has the UTF-8 BOM, it can't properly detect if a column called "id" exists if the first column is the id column.
        if (fileBytes.Length >= 3)
        {
            fileBytes = RemoveUtf8BomBytes(fileBytes);
        }

        // Turn the bytes into a string.
        var fileContents = Encoding.UTF8.GetString(fileBytes);
        var comboBoxFields = new List<ComboBoxDataModel>();
        var linkComboBoxFields = new Dictionary<int, List<ComboBoxDataModel>>();
        var properties = new List<(string PropertyName, string LanguageCode, string InputType, JObject Options)>();
        var linkProperties = new List<(string PropertyName, string LanguageCode, string InputType, JObject Options)>();

        // Get all properties for the entity type, so that we can check if the import file contains the correct columns and data types.
        clientDatabaseConnection.AddParameter("entityType", entityType);
        var dataTable = await clientDatabaseConnection.GetAsync($"""
                                                                 SELECT property_name, display_name, options, inputtype, data_query, language_code
                                                                 FROM {WiserTableNames.WiserEntityProperty}
                                                                 WHERE entity_name = ?entityType
                                                                 """);
        if (dataTable.Rows.Count > 0)
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var inputType = dataRow.Field<string>("inputtype");
                var propertyName = dataRow.Field<string>("property_name");
                var options = dataRow.Field<string>("options");
                var parsedOptions = JObject.Parse(String.IsNullOrWhiteSpace(options) ? "{}" : options);
                properties.Add((propertyName, dataRow.Field<string>("language_code"), inputType, parsedOptions));

                if (!inputType.Equals("combobox", StringComparison.OrdinalIgnoreCase) && !inputType.Equals("multiselect", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                comboBoxFields.Add(new ComboBoxDataModel
                {
                    PropertyName = propertyName,
                    DisplayName = dataRow.Field<string>("display_name"),
                    DataQuery = dataRow.Field<string>("data_query"),
                    Options = parsedOptions
                });
            }
        }

        var importData = new List<ImportDataModel>();

        // Process the data from the uploaded file, use IExcelService to read the file if it is an Excel file, otherwise use TextFieldParser to read the CSV file.
        if (importRequest.FilePath.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase))
        {
            var headerFields = excelService.GetColumnNames(importRequest.FilePath).ToArray();
            var (result, idIndex) = CheckHeader(headerFields, importResult);

            if (result != null)
            {
                return result;
            }

            var rows = excelService.GetLines(importRequest.FilePath,  headerFields.Length, true, true);
            var rowsHandled = 0;
            foreach (var row in rows.TakeWhile(row => rowsHandled <= ImportLimit))
            {
                await ProcessLineAsync(importResult, row.ToArray(), identity, moduleId, linkComboBoxFields, linkProperties, importData, idIndex, entityType, headerFields, importRequest, comboBoxFields, properties);
                rowsHandled++;
            }
        }
        else
        {
            using var stringReader = new StringReader(fileContents);
            using var textFieldParser = new TextFieldParser(stringReader);
            textFieldParser.Delimiters = [";"];
            textFieldParser.TextFieldType = FieldType.Delimited;
            textFieldParser.HasFieldsEnclosedInQuotes = true;

            string[] headerFields = null;
            var firstLine = true;
            var rowsHandled = 0;
            var idIndex = -1;

            while (!textFieldParser.EndOfData)
            {
                if (firstLine)
                {
                    headerFields = textFieldParser.ReadFields();
                    firstLine = false;
                    var headerResult = CheckHeader(headerFields, importResult);

                    if (headerResult.result != null)
                    {
                        return headerResult.result;
                    }

                    idIndex = headerResult.idIndex;
                    continue;
                }

                if (rowsHandled > ImportLimit)
                {
                    break;
                }

                rowsHandled += 1;

                var lineFields = textFieldParser.ReadFields();
                await ProcessLineAsync(importResult, lineFields, identity, moduleId, linkComboBoxFields, linkProperties, importData, idIndex, entityType, headerFields, importRequest, comboBoxFields, properties);
            }
        }

        // Don't do anything if we have errors.
        if (importResult.Failed > 0)
        {
            return new ServiceResult<ImportResultModel>(importResult);
        }

        var existingItemIds = new List<ulong>();
        var allItemIds = importData.Where(i => i.Item.Id > 0).Select(i => i.Item.Id).ToList();
        var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

        // Check if all items are of the correct entity type if there are any items that are imported according to the settings.
        if (allItemIds.Count != 0 && importRequest.ImportSettings.Count != 0)
        {
            dataTable = await clientDatabaseConnection.GetAsync($"SELECT id, entity_type FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE id IN ({String.Join(",", allItemIds)})");
            if (dataTable.Rows.Count > 0)
            {
                var allRows = dataTable.Rows.Cast<DataRow>().ToList();
                existingItemIds = allRows.Select(dataRow => dataRow.Field<ulong>("id")).ToList();

                var itemsWithWrongEntityType = allRows.Where(dataRow => !String.Equals(entityType, dataRow.Field<string>("entity_type"), StringComparison.OrdinalIgnoreCase)).Select(dataRow => $"{dataRow.Field<ulong>("id")} ({dataRow.Field<string>("entity_type")})").ToList();
                if (itemsWithWrongEntityType.Count != 0)
                {
                    importResult.Failed += 1U;
                    importResult.Errors.Add($"Can't do import because the following items have a different entity type than the selected type ({entityType}): {String.Join(", ", itemsWithWrongEntityType)}");
                    importResult.UserFriendlyErrors.Add($"Import kan niet gedaan worden omdat 1 of meer items niet het juiste entiteitstype hebben. U heeft gekozen om items van het type '{entityType}' te importeren, maar de volgende items hebben een ander type:<br>{String.Join(", ", itemsWithWrongEntityType)}");
                    return new ServiceResult<ImportResultModel>(importResult);
                }
            }
        }

        // Check if all links that the user is trying to import are valid.
        var itemIdsFromLinks = new List<ulong>();
        itemIdsFromLinks.AddRange(importData.SelectMany(i => i.Links.Select(l => l.ItemId)).Where(i => i > 0 && !itemIdsFromLinks.Contains(i)));
        itemIdsFromLinks.AddRange(importData.SelectMany(i => i.Links.Select(l => l.DestinationItemId)).Where(i => i > 0 && !itemIdsFromLinks.Contains(i)));
        if (itemIdsFromLinks.Count != 0)
        {
            allItemIds.AddRange(itemIdsFromLinks.Where(x => !allItemIds.Contains(x)));

            // First get all link type settings from database.
            var wiserLinkSettings = await wiserItemsService.GetAllLinkTypeSettingsAsync();

            // Create some variables for caching data.
            var tablePrefixes = new Dictionary<string, string>();
            var tablesWithItems = new Dictionary<string, List<ulong>>();

            foreach (var import in importData)
            {
                foreach (var link in import.Links)
                {
                    var sourceEntityType = link.ItemId == 0 ? entityType : null;
                    var destinationEntityType = link.DestinationItemId == 0 ? entityType : null;

                    LinkSettingsModel currentLinkSettings = null;
                    foreach (var linkSetting in wiserLinkSettings.Where(l => l.Type == link.Type))
                    {
                        // If the item ID is 0, it means we're creating a new item and linking that to the destination item. In that case, we need to check if the entity type of the source item is the same as the entity type in the link settings.
                        if (link.ItemId == 0 && !String.Equals(linkSetting.SourceEntityType, entityType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // If the destination item ID is 0, it means we're creating a new item and making that the parent of another item. In that case, we need to check if the entity type of the destination item is the same as the entity type in the link settings.
                        if (link.DestinationItemId == 0 && !String.Equals(linkSetting.DestinationEntityType, entityType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (link.ItemId > 0)
                        {
                            // Get the table prefix, if we don't know it yet for the current entity type.
                            if (!tablePrefixes.TryGetValue(linkSetting.SourceEntityType, out var sourceEntityTablePrefix))
                            {
                                sourceEntityTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSetting.SourceEntityType);
                                tablePrefixes.Add(linkSetting.SourceEntityType, sourceEntityTablePrefix);
                            }

                            if (!tablesWithItems.TryGetValue(sourceEntityTablePrefix, out var itemIds))
                            {
                                itemIds = [];
                                tablesWithItems.Add(sourceEntityTablePrefix, itemIds);
                            }

                            // Add the item ID for a temporary list, so that we can get all items at once later.
                            if (!itemIds.Contains(link.ItemId))
                            {
                                itemIds.Add(link.ItemId);
                            }
                        }

                        if (link.DestinationItemId <= 0)
                        {
                            continue;
                        }

                        // Get the table prefix, if we don't know it yet for the current entity type.
                        if (!tablePrefixes.TryGetValue(linkSetting.DestinationEntityType, out var destinationEntityTablePrefix))
                        {
                            destinationEntityTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSetting.DestinationEntityType);
                            tablePrefixes.Add(linkSetting.DestinationEntityType, destinationEntityTablePrefix);
                        }

                        if (!tablesWithItems.TryGetValue(destinationEntityTablePrefix, out var destinationItemIds))
                        {
                            destinationItemIds = ([]);
                            tablesWithItems.Add(destinationEntityTablePrefix, destinationItemIds);
                        }

                        // Add the item ID for a temporary list, so that we can get all items at once later.
                        if (!destinationItemIds.Contains(link.DestinationItemId))
                        {
                            destinationItemIds.Add(link.DestinationItemId);
                        }
                    }

                    if (sourceEntityType == null || destinationEntityType == null || currentLinkSettings == null)
                    {
                        // If one of the entity types is null, it means it doesn't exist. Then just continue, errors for non-existing items will be added later in the code.
                        continue;
                    }

                    if (currentLinkSettings != null)
                    {
                        if (!currentLinkSettings.UseItemParentId)
                        {
                            continue;
                        }

                        import.Item.ParentItemId = link.DestinationItemId;
                        link.UseParentItemId = true;
                    }
                    else
                    {
                        importResult.Failed++;
                        importResult.Errors.Add($"Trying to link item '{link.ItemId}' ({sourceEntityType}) to item '{link.DestinationItemId}' ({destinationEntityType}) with link type '{link.Type}', but this combination is not possible according to the settings in {WiserTableNames.WiserLink}.");
                        importResult.UserFriendlyErrors.Add($"Import kan niet gedaan worden omdat de volgende gekozen koppeling niet mogelijk is: Bron item = '{link.ItemId}', bron entiteit = '{entityType}', doel item = '{link.DestinationItemId}', doel entiteit = '{destinationEntityType}', linktype = '{link.Type}'<br>Indien u denkt dat dit niet klopt, neem dan contact op met ons.");
                    }
                }
            }

            // Check if all items exist in the database.
            foreach (var (currentTablePrefix, itemIds) in tablesWithItems.Where(x => x.Value.Count != 0))
            {
                dataTable = await clientDatabaseConnection.GetAsync($"SELECT id FROM {currentTablePrefix}{WiserTableNames.WiserItem} WHERE id IN ({String.Join(",", itemIds)})");
                if (dataTable.Rows.Count > 0)
                {
                    existingItemIds.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<ulong>("id")));
                }
            }
        }

        // Check if all files the user is trying to import are for existing items.
        var itemIdsFromFiles = importData.SelectMany(i => i.Files.Select(f => f.ItemId)).Where(i => i > 0 && !allItemIds.Contains(i)).ToList();
        if (itemIdsFromFiles.Count != 0)
        {
            allItemIds.AddRange(itemIdsFromFiles);
            dataTable = await clientDatabaseConnection.GetAsync($"SELECT id FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE id IN ({String.Join(",", itemIdsFromFiles)})");
            if (dataTable.Rows.Count > 0)
            {
                var allRows = dataTable.Rows.Cast<DataRow>().ToList();
                existingItemIds.AddRange(allRows.Select(dataRow => dataRow.Field<ulong>("id")));
            }
        }

        // Return error if 1 or more items don't exist.
        var missingItems = allItemIds.Except(existingItemIds).ToList();
        if (missingItems.Count != 0)
        {
            importResult.Failed += 1U;
            importResult.Errors.Add($"Can't do import because the following items don't exist: {String.Join(", ", missingItems)}");
            importResult.UserFriendlyErrors.Add($"Import kan niet gedaan worden omdat 1 of meer items niet bestaan of verwijderd zijn. Het gaat om de volgende items:<br>{String.Join(", ", missingItems)}");
        }

        // One or more errors occurred, skip the import and notify the user.
        if (importResult.Failed > 0)
        {
            return new ServiceResult<ImportResultModel>(importResult);
        }

        // All checks passed, we can now insert the import data into the database, so that the WTS can pick it up from here.
        var wiserImportId = 0;
        try
        {
            var json = JsonConvert.SerializeObject(importData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
            var parsedDate = importRequest.StartDate ?? DateTime.Now;

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", importRequest.Name);
            clientDatabaseConnection.AddParameter("start_on", parsedDate);
            clientDatabaseConnection.AddParameter("added_by", IdentityHelpers.GetUserName(identity, true));
            clientDatabaseConnection.AddParameter("added_on", DateTime.Now);
            clientDatabaseConnection.AddParameter("user_id", userId);
            clientDatabaseConnection.AddParameter("customer_id", tenant.TenantId);
            clientDatabaseConnection.AddParameter("data", json);
            clientDatabaseConnection.AddParameter("server_name", Environment.MachineName);
            clientDatabaseConnection.AddParameter("sub_domain", subDomain);
            wiserImportId = await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync<int>(WiserTableNames.WiserImport);
        }
        catch (Exception exception)
        {
            importResult.Failed += 1U;
            importResult.Errors.Add($"Failed to insert import data into database: {exception.Message}");
            importResult.UserFriendlyErrors.Add("De gegevens voor de import konden niet toegevoegd worden aan de database vanwege een onverwachte fout. Probeer het later opnieuw.");
            logger.LogError(exception, "Failed to insert import data into database.");
        }

        // Always add the import log, even if the import failed, so that we can see what went wrong.
        try
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("items_total", importResult.ItemsTotal);
            clientDatabaseConnection.AddParameter("items_created", importResult.ItemsCreated);
            clientDatabaseConnection.AddParameter("items_updated", importResult.ItemsUpdated);
            clientDatabaseConnection.AddParameter("items_successful", importResult.Successful);
            clientDatabaseConnection.AddParameter("items_failed", importResult.Failed);
            clientDatabaseConnection.AddParameter("errors", String.Join(Environment.NewLine, importResult.Errors));
            clientDatabaseConnection.AddParameter("added_on", DateTime.Now);
            clientDatabaseConnection.AddParameter("added_by", IdentityHelpers.GetUserName(identity, true));
            await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync<int>(WiserTableNames.WiserImportLog);
        }
        catch (Exception exception)
        {
            importResult.Failed += 1U;
            importResult.Errors.Add($"Failed to insert import log into database: {exception.Message}");
            importResult.UserFriendlyErrors.Add("De gegevens voor de import konden niet toegevoegd worden aan de database vanwege een onverwachte fout. Probeer het later opnieuw.");
            logger.LogError(exception, "Failed to insert import log into database.");
        }

        // If the import ID is 0, it means the import failed, and we can't continue.
        if (wiserImportId == 0)
        {
            return new ServiceResult<ImportResultModel>(importResult);
        }

        if (String.IsNullOrWhiteSpace(importRequest.ImagesFileName) && String.IsNullOrWhiteSpace(importRequest.ImagesFilePath))
        {
            return new ServiceResult<ImportResultModel>(importResult);
        }

        // Extract the images zip to a place where the WTS can find them.
        var basePath = $@"C:\temp\WTS Import\{tenant.TenantId}\{wiserImportId}\";

        var imagesDirectory = new DirectoryInfo(basePath);
        imagesDirectory.Create();

        try
        {
            CompressionHelpers.ExtractZipFile(importRequest.ImagesFilePath, imagesDirectory.FullName);
        }
        catch (Exception exception)
        {
            importResult.Failed += 1U;
            importResult.Errors.Add($"Failed to extract images from import zip file: {exception.Message}");
            importResult.UserFriendlyErrors.Add("Het is niet gelukt om de afbeeldingen uit het import zip-bestand te halen. Controleer of het zip-bestand geldig is en probeer het opnieuw.");
            logger.LogError(exception, "Failed to extract images from import zip file.");
        }

        return new ServiceResult<ImportResultModel>(importResult);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DeleteItemsConfirmModel>> PrepareDeleteItemsAsync(ClaimsIdentity identity, DeleteItemsRequestModel deleteItemsRequest)
    {
        // Get all lines, skip first line containing column names.
        var fileLines = (await File.ReadAllLinesAsync(deleteItemsRequest.FilePath)).Skip(1);

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("entityName", deleteItemsRequest.EntityName);
        clientDatabaseConnection.AddParameter("propertyName", deleteItemsRequest.PropertyName);

        var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(deleteItemsRequest.EntityName);

        // Build the query based on a delete by id or by property.
        var whereClause = deleteItemsRequest.PropertyName == "id"
            ? CreatePrepareDeleteQueryBottomForId(fileLines)
            : CreatePrepareDeleteQueryBottomForProperty(fileLines, tablePrefix);
        var query = $"""
                     SELECT item.id
                     FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                     {whereClause}
                     """;
        var dataTable = await clientDatabaseConnection.GetAsync(query);

        var itemsToDelete = new DeleteItemsConfirmModel
        {
            EntityType = deleteItemsRequest.EntityName,
            Ids = []
        };

        foreach (DataRow dataRow in dataTable.Rows)
        {
            itemsToDelete.Ids.Add(dataRow.Field<ulong>("id"));
        }

        return new ServiceResult<DeleteItemsConfirmModel>(itemsToDelete);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteItemsAsync(ClaimsIdentity identity, DeleteItemsConfirmModel deleteItemsConfirm)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        var userId = IdentityHelpers.GetWiserUserId(identity);
        var userName = $"Import ({IdentityHelpers.GetName(identity)})";

        await wiserItemsService.DeleteAsync(deleteItemsConfirm.Ids, userId: userId, username: userName, entityType: deleteItemsConfirm.EntityType);
        return new ServiceResult<bool>(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<DeleteLinksConfirmModel>>> PrepareDeleteLinksAsync(ClaimsIdentity identity, DeleteLinksRequestModel deleteLinksRequest)
    {
        // Get all lines, skip first line containing column names.
        var fileLines = (await File.ReadAllLinesAsync(deleteLinksRequest.FilePath)).Skip(1).ToList();
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var linksToDelete = new List<DeleteLinksConfirmModel>();

        switch (deleteLinksRequest.DeleteLinksType)
        {
            case DeleteLinksTypes.Single:
                linksToDelete.Add(await CreateQueryForSingleColumn(fileLines, deleteLinksRequest));
                break;
            case DeleteLinksTypes.Multiple:
                linksToDelete.AddRange(await CreateQueryForMultipleColumns(fileLines, deleteLinksRequest));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(deleteLinksRequest.DeleteLinksType), deleteLinksRequest.DeleteLinksType.ToString(), null);
        }

        return new ServiceResult<List<DeleteLinksConfirmModel>>(linksToDelete);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteLinksAsync(ClaimsIdentity identity, List<DeleteLinksConfirmModel> deleteLinksConfirms)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        try
        {
            var userId = IdentityHelpers.GetWiserUserId(identity);
            var userName = $"Import ({IdentityHelpers.GetName(identity)})";

            await clientDatabaseConnection.BeginTransactionAsync();

            foreach (var deleteLinksConfirm in deleteLinksConfirms)
            {
                if (deleteLinksConfirm.Ids.Count == 0)
                {
                    continue;
                }

                if (deleteLinksConfirm.UseParentId)
                {
                    await wiserItemsService.RemoveParentLinkOfItemsAsync(deleteLinksConfirm.Ids, deleteLinksConfirm.SourceEntityType, deleteLinksConfirm.SourceIds, deleteLinksConfirm.DestinationEntityType, deleteLinksConfirm.DestinationIds, userName, userId);
                }
                else
                {
                    await wiserItemsService.RemoveItemLinksByIdAsync(deleteLinksConfirm.Ids, deleteLinksConfirm.SourceEntityType, deleteLinksConfirm.SourceIds, deleteLinksConfirm.DestinationEntityType, deleteLinksConfirm.DestinationIds, userName, userId);
                }
            }

            await clientDatabaseConnection.CommitTransactionAsync();

            return new ServiceResult<bool>(true);
        }
        catch
        {
            await clientDatabaseConnection.RollbackTransactionAsync(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IEnumerable<EntityPropertyModel>>> GetEntityPropertiesAsync(ClaimsIdentity identity, string entityName = null, int linkType = 0)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("entityName", entityName ?? String.Empty);
        clientDatabaseConnection.AddParameter("linkType", linkType);

        const string query = $"""
                              SELECT property.id, property.display_name, property.property_name, property.language_code, property.inputtype, property.`options`, property.ordering
                              FROM (
                                  SELECT
                                      0 AS id,
                                      'Item naam' AS display_name,
                                      'itemTitle' AS property_name,
                                      '' AS language_code,
                                      'input' AS inputtype,
                                      '' AS `options`,
                                      1 AS ordering,
                                      0 AS base_order
                                  FROM DUAL
                                  WHERE ?entityName <> ''
                                  UNION
                                  SELECT
                                      id,
                                      CONCAT(
                                          IF(display_name = '', property_name, display_name),
                                          IF(
                                              language_code <> '',
                                              CONCAT(' (', language_code, ')'),
                                              ''
                                          )
                                      ) AS display_name,
                                      IF(property_name = '', display_name, property_name) AS property_name,
                                      language_code,
                                      inputtype,
                                      IF(inputtype = 'image-upload', `options`, '') AS `options`,
                                      ordering AS ordering,
                                      1 AS base_order
                                  FROM `{WiserTableNames.WiserEntityProperty}`
                                  WHERE entity_name = ?entityName OR (?linkType > 0 AND link_type = ?linkType)
                                  ORDER BY base_order, display_name
                              ) AS property
                              """;
        var getPropertiesResult = await clientDatabaseConnection.GetAsync(query);

        if (getPropertiesResult.Rows.Count == 0)
        {
            return new ServiceResult<IEnumerable<EntityPropertyModel>>(null);
        }

        var entityProperties = new List<EntityPropertyModel>(getPropertiesResult.Rows.Count);
        entityProperties.AddRange(getPropertiesResult.Rows.Cast<DataRow>()
            .Select(entityPropertyDataRow => new EntityPropertyModel
            {
                Id = Convert.ToInt32(entityPropertyDataRow["id"]),
                DisplayName = entityPropertyDataRow.Field<string>("display_name"),
                PropertyName = entityPropertyDataRow.Field<string>("property_name"),
                LanguageCode = entityPropertyDataRow.Field<string>("language_code"),
                InputType = EntityPropertyHelper.ToInputType(entityPropertyDataRow.Field<string>("inputtype")),
                Options = entityPropertyDataRow.Field<string>("options") ?? String.Empty,
                Ordering = Convert.ToInt32(entityPropertyDataRow["ordering"])
            }));

        return new ServiceResult<IEnumerable<EntityPropertyModel>>(entityProperties);
    }

    /// <summary>
    /// Check the header of the file to see if it contains an ID column.
    /// </summary>
    /// <param name="headerFields">The fields containing the headers.</param>
    /// <param name="importResult">The result that will be given back to the front-end to set any errors if they occur.</param>
    /// <returns>Returns the index of the ID column and depending on if it was found the <see cref="ServiceResult{T}"/> to return.</returns>
    private static (ServiceResult<ImportResultModel> result, int idIndex) CheckHeader(string[] headerFields, ImportResultModel importResult)
    {
        var idIndex = Array.FindIndex(headerFields, s => s.Equals("id", StringComparison.OrdinalIgnoreCase));

        if (idIndex >= 0)
        {
            return (null, idIndex);
        }

        importResult.Failed += 1U;
        importResult.Errors.Add("Can't do import because of missing ID column");
        importResult.UserFriendlyErrors.Add("De import kan niet gedaan worden omdat er geen kolom genaamd 'id' is gevonden in het importbestand. Er moet altijd een kolom met de naam 'id' zijn. Bij het wijzigen van bestaande items, moet daar het ID van het item im komen te staan. Bij het toevoegen van nieuwe items, kan de kolon leeg blijven, of '0' zijn. Bij het importeren van koppelingen moet daar het ID van een van de items in staan (en het andere ID moet dan in een andere kolom staan).");
        return (new ServiceResult<ImportResultModel>(importResult), idIndex);
    }

    /// <summary>
    /// Process a given line to determine the import action.
    /// </summary>
    /// <param name="importResult">The result to add the information to of the line.</param>
    /// <param name="lineFields">The fields/columns in the line to process.</param>
    /// <param name="identity">The identity of the logged-in user.</param>
    /// <param name="moduleId">The ID of the module the item will be created in.</param>
    /// <param name="linkComboBoxFields">A list of <see cref="ComboBoxDataModel"/>s to add information of the combobox for linked items to.</param>
    /// <param name="linkProperties">A list of properties to add information of properties on the link to.</param>
    /// <param name="importData">A list of <see cref="ImportDataModel"/>s containing the data that need to be imported.</param>
    /// <param name="idIndex">The index of the ID column.</param>
    /// <param name="entityType">The entity type being imported.</param>
    /// <param name="headerFields">The names of the columns in the header.</param>
    /// <param name="importRequest">The <see cref="ImportRequestModel"/> with the information filled in Wiser.</param>
    /// <param name="comboBoxFields">The information of combobox fields in the entity to be set.</param>
    /// <param name="properties">The information of the properties in the entity to be set.</param>
    private async Task ProcessLineAsync(ImportResultModel importResult,
        string[] lineFields,
        ClaimsIdentity identity,
        int moduleId,
        Dictionary<int, List<ComboBoxDataModel>> linkComboBoxFields,
        List<(string PropertyName, string LanguageCode, string InputType, JObject Options)> linkProperties,
        List<ImportDataModel> importData,
        int idIndex,
        string entityType,
        string[] headerFields,
        ImportRequestModel importRequest,
        List<ComboBoxDataModel> comboBoxFields,
        List<(string PropertyName, string LanguageCode, string InputType, JObject Options)> properties)
    {
        if (lineFields.All(String.IsNullOrWhiteSpace))
        {
            // Don't import empty rows.
            return;
        }

        var importItem = new ImportDataModel
        {
            Item = new WiserItemModel
            {
                ChangedBy = IdentityHelpers.GetUserName(identity, true),
                ModuleId = moduleId,
                EntityType = entityType
            }
        };
        importData.Add(importItem);

        var isNewItem = true;

        if (idIndex >= 0 && !String.IsNullOrWhiteSpace(lineFields[idIndex]))
        {
            // ID field exists and is filled; line is item update.
            importItem.Item.Id = Convert.ToUInt64(lineFields[idIndex]);
            isNewItem = importItem.Item.Id == 0;
        }

        // Now update the item with the fields from the import.
        var images = new List<ImageUploadSettingsModel>();

        // Item details and item links.
        for (var i = 0; i <= lineFields.Length - 1; i++)
        {
            var importColumnName = headerFields[i];

            // Ignore ID column.
            if (importColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lineSettings = importRequest.ImportSettings.FirstOrDefault(d => d["column"]?.ToString() == importColumnName);
            var linkSettings = importRequest.ImportLinkSettings.FirstOrDefault(d => d["column"]?.ToString() == importColumnName);

            if (lineSettings != null)
            {
                var propertyName = lineSettings["propertyName"] as string;
                var isImageField = (bool)lineSettings["isImageField"];
                var allowMultipleImages = (bool)lineSettings["allowMultipleImages"];
                var languageCode = lineSettings["languageCode"] as string ?? "";

                if (isImageField)
                {
                    if (!String.IsNullOrWhiteSpace(importRequest.ImagesFileName))
                    {
                        images.Add(new ImageUploadSettingsModel { PropertyName = propertyName, FilePath = lineFields[i], AllowMultipleImages = allowMultipleImages });
                    }
                }
                else if (!String.IsNullOrWhiteSpace(propertyName))
                {
                    // TODO: Also handle unique_uuid, published_environment etc?
                    if (propertyName.Equals("itemTitle", StringComparison.OrdinalIgnoreCase))
                    {
                        importItem.Item.Title = lineFields[i];
                    }
                    else
                    {
                        var value = await HandleComboBoxFieldAsync(comboBoxFields, languageCode, importItem, importItem.Item, propertyName, lineFields[i], importResult, false);
                        if (!HandleFieldValue(properties, propertyName, languageCode, importResult, importColumnName, ref value))
                        {
                            continue;
                        }

                        var itemDetail = new WiserItemDetailModel
                        {
                            Key = propertyName,
                            Value = value,
                            LanguageCode = languageCode
                        };

                        importItem.Item.SetDetail(itemDetail);
                    }
                }
            }

            if (linkSettings == null)
            {
                continue;
            }

            foreach (var value in lineFields[i].Split(','))
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var itemLink = new ItemLinkImportModel
                {
                    Type = Convert.ToInt32(linkSettings["linkType"]),
                    DeleteExistingLinks = Convert.ToBoolean(linkSettings["deleteExistingLinks"])
                };

                if (Convert.ToBoolean(linkSettings["linkIsDestination"]))
                {
                    // Link newly made item to item in settings.
                    itemLink.ItemId = importItem.Item.Id;
                    itemLink.DestinationItemId = Convert.ToUInt64(value.Trim());
                }
                else
                {
                    // Link item in settings to newly made item.
                    itemLink.ItemId = Convert.ToUInt64(value.Trim());
                    itemLink.DestinationItemId = importItem.Item.Id;
                }

                // Make sure an item won't get linked to itself.
                if (itemLink.ItemId == itemLink.DestinationItemId)
                {
                    continue;
                }

                // Make sure that we don't import duplicate links.
                if (importItem.Links.Any(x => x.ItemId == itemLink.ItemId && x.DestinationItemId == itemLink.DestinationItemId && x.Type == itemLink.Type))
                {
                    continue;
                }

                importItem.Links.Add(itemLink);
            }
        }

        // Item link details. We do this in a separate loop to make sure that all links are known, before adding details to them, so that the order of the columns in the CSV does not matter.
        for (var i = 0; i <= lineFields.Length - 1; i++)
        {
            var importColumnName = headerFields[i];

            // Ignore ID column.
            if (importColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var linkDetailSettings = importRequest.ImportLinkDetailSettings.FirstOrDefault(d => d["column"]?.ToString() == importColumnName);
            if (linkDetailSettings == null)
            {
                continue;
            }

            var itemLink = importItem.Links.FirstOrDefault(l => l.Type == Convert.ToInt32(linkDetailSettings["linkType"]));
            if (itemLink == null)
            {
                continue;
            }

            if (!linkComboBoxFields.TryGetValue(itemLink.Type, out var currentLinkComboBoxFields))
            {
                currentLinkComboBoxFields = [];
                linkComboBoxFields.Add(itemLink.Type, currentLinkComboBoxFields);
                clientDatabaseConnection.AddParameter("linkType", itemLink.Type);
                var dataTable = await clientDatabaseConnection.GetAsync($"""
                                                                         SELECT property_name, display_name, options, inputtype, data_query, language_code
                                                                         FROM {WiserTableNames.WiserEntityProperty}
                                                                         WHERE link_type = ?linkType
                                                                         """);
                if (dataTable.Rows.Count > 0)
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var inputType = dataRow.Field<string>("inputtype");
                        var databasePropertyName = dataRow.Field<string>("property_name");
                        var options = dataRow.Field<string>("options");
                        var parsedOptions = JObject.Parse(String.IsNullOrWhiteSpace(options) ? "{}" : options);
                        linkProperties.Add((databasePropertyName, dataRow.Field<string>("language_code"), inputType, parsedOptions));

                        if (!inputType.Equals("combobox", StringComparison.OrdinalIgnoreCase) && !inputType.Equals("multiselect", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        linkComboBoxFields[itemLink.Type].Add(new ComboBoxDataModel
                        {
                            PropertyName = databasePropertyName,
                            DisplayName = dataRow.Field<string>("display_name"),
                            DataQuery = dataRow.Field<string>("data_query"),
                            Options = JObject.Parse(String.IsNullOrWhiteSpace(options) ? "{}" : options)
                        });
                    }
                }
            }

            var propertyName = linkDetailSettings["propertyName"] as string;
            var isImageField = (bool)linkDetailSettings["isImageField"];
            var allowMultipleImages = (bool)linkDetailSettings["allowMultipleImages"];
            var languageCode = linkDetailSettings["languageCode"] as string ?? "";

            if (isImageField)
            {
                if (!String.IsNullOrWhiteSpace(importRequest.ImagesFileName))
                {
                    images.Add(new ImageUploadSettingsModel { PropertyName = propertyName, FilePath = lineFields[i], AllowMultipleImages = allowMultipleImages });
                }
            }
            else if (!String.IsNullOrWhiteSpace(propertyName))
            {
                var value = await HandleComboBoxFieldAsync(currentLinkComboBoxFields, languageCode, importItem, itemLink, propertyName, lineFields[i], importResult, true);
                if (!HandleFieldValue(properties, propertyName, languageCode, importResult, importColumnName, ref value))
                {
                    continue;
                }

                var itemDetail = new WiserItemDetailModel
                {
                    IsLinkProperty = true,
                    Key = propertyName,
                    Value = value,
                    LanguageCode = languageCode
                };

                itemLink.SetDetail(itemDetail);
            }
        }

        importResult.ItemsTotal += 1U;

        try
        {
            // Import files.
            if (images is {Count: > 0})
            {
                foreach (var image in images)
                {
                    var itemFile = new WiserItemFileModel
                    {
                        ItemId = importItem.Item.Id,
                        FileName = Path.GetFileName(image.FilePath),
                        Extension = Path.GetExtension(image.FilePath),
                        AddedBy = IdentityHelpers.GetUserName(identity, true),
                        PropertyName = image.PropertyName
                    };
                    importItem.Files.Add(itemFile);

                    if (isNewItem || image.AllowMultipleImages)
                    {
                        continue;
                    }

                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("item_id", importItem.Item.Id);
                    clientDatabaseConnection.AddParameter("property_name", image.PropertyName);
                    var dataTable = await clientDatabaseConnection.GetAsync($"SELECT id FROM {WiserTableNames.WiserItemFile} WHERE item_id = ?item_id AND property_name = ?property_name LIMIT 1");
                    if (dataTable.Rows.Count > 0)
                    {
                        itemFile.Id = Convert.ToUInt64(dataTable.Rows[0]["id"]);
                    }
                }
            }

            // No errors here means it was successful.
            importResult.Successful += 1U;

            if (isNewItem)
            {
                importResult.ItemsCreated += 1U;
            }
            else
            {
                importResult.ItemsUpdated += 1U;
            }
        }
        catch (Exception exception)
        {
            importResult.Failed += 1U;
            importResult.Errors.Add($"Failed to import item with ID '{importItem.Item.Id}': {exception.Message}");
            importResult.UserFriendlyErrors.Add($"Het item met ID '{importItem.Item.Id}' kon niet geïmporteerd worden vanwege een onverwachte fout. Probeer het later opnieuw.");
            logger.LogError(exception, "Failed to import item with ID '{ItemId}'", importItem.Item.Id);
        }
    }

    /// <summary>
    /// Validates and handles the value of a field based on its input type.
    /// This will add any validation errors to the <see cref="ImportResultModel"/> if the value is invalid.
    /// It will also convert the value to the correct format for Wiser, if necessary.
    /// </summary>
    /// <param name="properties">The complete list of properties of the specified entity type.</param>
    /// <param name="propertyName">The name of the property/field to handle.</param>
    /// <param name="languageCode">The language code of the property/field.</param>
    /// <param name="importResult">The <see cref="ImportResultModel"/> to store the final result in.</param>
    /// <param name="importColumnName">The column name in the import file.</param>
    /// <param name="value">The value from the import file.</param>
    /// <returns><c>true</c> if the value was handled successfully, <c>false</c> if it was not valid and should not be imported.</returns>
    private static bool HandleFieldValue(List<(string PropertyName, string LanguageCode, string InputType, JObject Options)> properties, string propertyName, string languageCode, ImportResultModel importResult, string importColumnName, ref string value)
    {
        var property = properties.FirstOrDefault(p => String.Equals(p.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase) && String.Equals(p.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
        switch (property.InputType?.ToLowerInvariant() ?? "input")
        {
            case "secure-input":
            case "file-upload":
            case "querybuilder":
            case "button":
            case "image-upload":
            case "item-linker":
            case "auto-increment":
            case "linked-item":
            case "action-button":
            case "chart":
            case "scheduler":
            case "timeline":
            case "empty":
                importResult.Failed += 1U;
                importResult.Errors.Add($"Field '{propertyName}' is a {property.InputType}. These cannot be imported.");
                importResult.UserFriendlyErrors.Add($"Het veld '{importColumnName}' is een veld van het type '{property.InputType}' in Wiser. Dit soort velden kunnen niet geïmporteerd worden.");
                return false;
            case "checkbox":
                var isValidNumber = Int32.TryParse(value, out var parsedNumber);
                var isValidBoolean = Boolean.TryParse(value, out var parsedBoolean);
                switch (isValidBoolean)
                {
                    case false when (!isValidNumber || parsedNumber < 0 || parsedNumber > 1):
                        importResult.Failed += 1U;
                        importResult.Errors.Add($"Field '{propertyName}' is a {property.InputType}, but contains an invalid value ({value}).");
                        importResult.UserFriendlyErrors.Add($"Het veld '{importColumnName}' is een checkboxveld in Wiser en bevat een ongeldige waarde ({value}). Een checkbox moet een van de volgende waarden bevatten: '0', '1', 'true' of 'false'.");
                        return true;
                    case true:
                        value = parsedBoolean ? "1" : "0";
                        break;
                }

                break;
            case "numeric-input":
                if (String.IsNullOrWhiteSpace(value))
                {
                    value = "0";
                }

                if (!Decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out var parsedDecimal))
                {
                    importResult.Failed += 1U;
                    importResult.Errors.Add($"Field '{propertyName}' is a {property.InputType}, but contains an invalid value ({value}).");
                    importResult.UserFriendlyErrors.Add($"Het veld '{importColumnName}' is een nummerveld in Wiser en bevat een ongeldige waarde ({value}).");
                    return false;
                }

                // Make sure that the number is saved in the correct format for Wiser.
                value = parsedDecimal.ToString(new CultureInfo("en-US"));
                break;
            case "date-time picker":
                if (String.IsNullOrEmpty(value))
                {
                    // Don't do anything, empty values are allowed.
                    break;
                }

                if (!DateTime.TryParse(value, new CultureInfo("nl-NL"), DateTimeStyles.AssumeLocal, out var parsedDateTime))
                {
                    importResult.Failed += 1U;
                    importResult.Errors.Add($"Field '{propertyName}' is a {property.InputType}, but contains an invalid value ({value}).");
                    importResult.UserFriendlyErrors.Add($"Het veld '{importColumnName}' is een datumveld in Wiser en bevat een ongeldige waarde ({value}). Datumvelden moeten in een van de volgende formaten geïmporteerd worden: dag-maand-jaar uren:minuten:seconden, dag-maand-jaar uren:minuten, dag-maand-jaar, jaar-maand-dag uren:minuten:seconden, jaar-maand-dag uren:minuten, jaar-maand-dag");
                    return false;
                }

                value = (property.Options.Value<string>("type")?.ToLowerInvariant() ?? "") switch
                {
                    "date" => parsedDateTime.ToString("yyyy-MM-dd"),
                    "time" => parsedDateTime.ToString("HH:mm:ss"),
                    _ => parsedDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                };

                break;
        }

        return true;
    }

    /// <summary>
    /// Checks if the first three bytes of a byte array are the same as the UTF-8 BOM. If they are, they are removed.
    /// </summary>
    /// <param name="fileBytes">The byte array to use.</param>
    /// <returns>The byte array without UTF-8 BOM bytes.</returns>
    private static byte[] RemoveUtf8BomBytes(byte[] fileBytes)
    {
        var utf8BomBytes = new byte[] { 0xEF, 0xBB, 0xBF };
        if (fileBytes.Length < 3 || !fileBytes.Take(3).SequenceEqual(utf8BomBytes))
        {
            return fileBytes;
        }

        // File has UTF-8 BOM bytes, remove them.
        var newByteArray = new byte[fileBytes.Length - 1 + 1];
        Array.Copy(fileBytes, newByteArray, fileBytes.Length);
        newByteArray = newByteArray.Skip(3).ToArray();

        return newByteArray;
    }

    /// <summary>
    /// Validates and handles the value of a combobox field.
    /// This will add any validation errors to the <see cref="ImportResultModel"/> if the value is invalid.
    /// It will also convert the value to the correct format for Wiser, if necessary.
    /// </summary>
    /// <param name="comboBoxFields">The complete list of all combobox fields of the specified entity type.</param>
    /// <param name="languageCode">The language code of the property/field.</param>
    /// <param name="importItem">The <see cref="ImportDataModel"/> of the row/item that is being handled.</param>
    /// <param name="details">The list of <see cref="WiserItemDetailModel"/> to import.</param>
    /// <param name="propertyName">The name of the property/field to handle.</param>
    /// <param name="value">The value from the import file.</param>
    /// <param name="importResult">The <see cref="ImportResultModel"/> to store the final result in.</param>
    /// <param name="isLinkProperty">Whether the current property is for wiser_itemlinkdetail instead of wiser_itemdetail.</param>
    /// <returns>The value to save in wiser_itemdetail or wiser_itemlinkdetail.</returns>
    private async Task<string> HandleComboBoxFieldAsync(List<ComboBoxDataModel> comboBoxFields,
        string languageCode,
        ImportDataModel importItem,
        WiserItemBaseModel detailsItem,
        string propertyName,
        string value,
        ImportResultModel importResult,
        bool isLinkProperty)
    {
        // If this is a property with input type combobox or multi select, then allow the users to import the text value. We will look up the corresponding ID and import that ID.
        var comboBoxField = comboBoxFields.FirstOrDefault(x => String.Equals(x.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
        if (comboBoxField == null)
        {
            return value;
        }

        await AddComboBoxValuesAsync(comboBoxField);
        var allValues = value.Split(',');
        var ids = new List<string>();
        var names = new List<string>();

        foreach (var currentValue in allValues)
        {
            if (String.IsNullOrWhiteSpace(currentValue))
            {
                continue;
            }

            var keyValuePair = comboBoxField.Values.FirstOrDefault(v => String.Equals(v.Key, currentValue, StringComparison.OrdinalIgnoreCase) || String.Equals(v.Value, currentValue, StringComparison.OrdinalIgnoreCase));
            if (String.IsNullOrWhiteSpace(keyValuePair.Key))
            {
                importResult.Failed += 1U;
                importResult.Errors.Add($"Value '{currentValue}' not found in list of possible values for comboBox '{comboBoxField.PropertyName}'");
                importResult.UserFriendlyErrors.Add($"De waarde '{currentValue}' kan niet opgeslagen worden in het veld '{comboBoxField.DisplayName}', omdat dit veld een combobox is en deze waarde niet voorkomt in de lijst van mogelijke waardes.");
                return value;
            }

            ids.Add(keyValuePair.Key);
            names.Add(keyValuePair.Value);
        }

        detailsItem.SetDetail(new WiserItemDetailModel
        {
            IsLinkProperty = isLinkProperty,
            Key = propertyName + "_input",
            Value = String.Join(",", names),
            LanguageCode = languageCode
        });

        if (isLinkProperty || !comboBoxField.Options.Value<bool>("saveValueAsItemLink"))
        {
            value = String.Join(",", ids);
        }
        else
        {
            value = null;
            var currentItemIsDestinationId = comboBoxField.Options.Value<bool>("currentItemIsDestinationId");
            var linkTypeNumber = comboBoxField.Options.Value<int>("linkTypeNumber");
            foreach (var idToLink in ids)
            {
                var otherItemId = UInt64.Parse(idToLink);
                importItem.Links.Add(new ItemLinkImportModel
                {
                    DestinationItemId = currentItemIsDestinationId ? importItem.Item.Id : otherItemId,
                    Type = linkTypeNumber,
                    ItemId = currentItemIsDestinationId ? otherItemId : importItem.Item.Id
                });
            }
        }


        return value;
    }

    /// <summary>
    /// Create a list of values that a combobox can have.
    /// This will use the settings of the combobox to determine how to get the values.
    /// </summary>
    /// <param name="comboBox">The combobox to get the values for.</param>
    /// <exception cref="NotImplementedException">When using a combobox that uses a data selector, which isn't supported yet.</exception>
    private async Task AddComboBoxValuesAsync(ComboBoxDataModel comboBox)
    {
        comboBox.Values ??= new Dictionary<string, string>();
        if (comboBox.Values.Count > 0)
        {
            // Already have the values, don't get them again.
            return;
        }

        var entityType = comboBox.Options.Value<string>("entityType");
        var dataSelectorId = comboBox.Options.Value<string>("dataSelectorId");
        var textField = comboBox.Options.Value<string>("dataTextField");
        var valueField = comboBox.Options.Value<string>("dataValueField");
        if (String.IsNullOrWhiteSpace(textField))
        {
            textField = "name";
        }

        if (String.IsNullOrWhiteSpace(valueField))
        {
            valueField = "id";
        }

        if (!String.IsNullOrWhiteSpace(entityType))
        {
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
            clientDatabaseConnection.AddParameter("comboBoxEntityType", entityType);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT id, title FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE entity_type = ?comboBoxEntityType AND published_environment > 0");
            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                comboBox.Values.Add(Convert.ToString(dataRow["id"])!, dataRow.Field<string>("title"));
            }
        }
        else if (!String.IsNullOrWhiteSpace(comboBox.DataQuery))
        {
            var dataTable = await clientDatabaseConnection.GetAsync(comboBox.DataQuery);
            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull(valueField) || dataRow.IsNull(textField))
                {
                    continue;
                }

                comboBox.Values.Add(Convert.ToString(dataRow[valueField])!, Convert.ToString(dataRow[textField]));
            }
        }
        else if (comboBox.Options.ContainsKey("dataSource"))
        {
            var dataSourceString = comboBox.Options["dataSource"]?.ToString();
            if (comboBox.Options["dataSource"] is JArray jsonArray)
            {
                foreach (var jsonToken in jsonArray)
                {
                    comboBox.Values.Add(jsonToken.Value<string>("id"), jsonToken.Value<string>("name"));
                }
            }
            else if (dataSourceString == "wiserUsers")
            {
                var users = await usersService.GetAsync();
                foreach (var backendUser in users.ModelObject)
                {
                    comboBox.Values.Add(backendUser.Id.ToString(), backendUser.Title);
                }
            }
        }
        else if (!String.IsNullOrWhiteSpace(dataSelectorId))
        {
            throw new NotImplementedException("TODO: Use data selector via GCL once it's implemented. Don't call get_items.jcl, that creates unneeded extra overhead.");
            /*var wiser2DataUrl = tenant.Wiser2DataUrl;
            var restClient = new RestClient(wiser2DataUrl);

            var restRequest = new RestRequest($"get_items.jcl?dataselectorid={dataSelectorId.EncryptWithAesWithSalt(tenant.EncryptionKey, true)}&trace=false", Method.GET);

            var restResponse = await restClient.ExecuteAsync(restRequest);
            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            var dataSelectorResult = JToken.Parse(restResponse.Content);
            if (!(dataSelectorResult is JArray jsonArray))
            {
                return;
            }

            foreach (var jsonToken in jsonArray)
            {
                comboBox.Values.Add(jsonToken.Value<string>(valueField), jsonToken.Value<string>(textField));
            }*/
        }
    }

    /// <summary>
    /// Create the bottom of the query to find what items to delete based on id.
    /// </summary>
    /// <param name="fileLines"></param>
    /// <returns>Returns the query for after the SELECT statement for ids.</returns>
    private static string CreatePrepareDeleteQueryBottomForId(IEnumerable<string> fileLines)
    {
        return $"""
                WHERE item.id IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))})
                AND item.entity_type = ?entityName
                """;
    }

    /// <summary>
    /// Create the bottom of the query to find what items to delete based on an item's property.
    /// </summary>
    /// <param name="fileLines"></param>
    /// <param name="tablePrefix">The prefix of the table where the items will be deleted</param>
    /// <returns>Returns the query for after the SELECT statement for an item's property</returns>
    private static string CreatePrepareDeleteQueryBottomForProperty(IEnumerable<string> fileLines, string tablePrefix)
    {
        return $"""
                JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS detail ON detail.item_id = item.id AND detail.`key` = ?propertyName
                WHERE detail.`value` IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))})
                AND item.entity_type = ?entityName
                """;
    }

    /// <summary>
    /// Create the query to find what item links to delete for single column.
    /// </summary>
    /// <param name="fileLines"></param>
    /// <param name="deleteLinksRequest">The criteria for the item links to delete.</param>
    /// <returns>Returns the <see cref="DeleteLinksConfirmModel"/> containing the information to delete the links.</returns>
    private async Task<DeleteLinksConfirmModel> CreateQueryForSingleColumn(IList<string> fileLines, DeleteLinksRequestModel deleteLinksRequest)
    {
        var linkSettings = await wiserItemsService.GetLinkTypeSettingsByIdAsync(deleteLinksRequest.LinkId);
        var destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSettings.DestinationEntityType);
        var connectedTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSettings.SourceEntityType);
        var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(0, linkSettings.SourceEntityType, linkSettings.DestinationEntityType);

        clientDatabaseConnection.AddParameter("linkId", deleteLinksRequest.LinkId);

        var query = linkSettings.UseItemParentId
            ? $"""
               SELECT connected.id AS id, connected.id AS sourceId, destination.id AS destinationId
               FROM {linkTablePrefix}{WiserTableNames.WiserLink} AS linkSettings
               JOIN {connectedTablePrefix}{WiserTableNames.WiserItem} AS connected ON connected.entity_type = linkSettings.connected_entity_type
               JOIN {destinationTablePrefix}{WiserTableNames.WiserItem} AS destination ON destination.id = connected.parent_item_id AND destination.entity_type = linkSettings.destination_entity_type
               WHERE linkSettings.id = ?linkId
               AND (connected.id IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))}) OR connected.parent_item_id IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))}))
               """
            : $"""
               SELECT itemLink.id AS id, connected.id AS sourceId, destination.id AS destinationId
               FROM {WiserTableNames.WiserLink} AS linkSettings
               JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS itemLink ON itemLink.type = linkSettings.type
               JOIN {destinationTablePrefix}{WiserTableNames.WiserItem} AS destination ON destination.id = itemLink.destination_item_id AND destination.entity_type = linkSettings.destination_entity_type
               JOIN {connectedTablePrefix}{WiserTableNames.WiserItem} AS connected ON connected.id = itemLink.item_id AND connected.entity_type = linkSettings.connected_entity_type
               WHERE linkSettings.id = ?linkId
               AND (itemLink.destination_item_id IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))}) OR itemLink.item_id IN({String.Join(",", fileLines.Select(line => line.ToMySqlSafeValue(true)))}))
               """;

        return await CreateDeleteLinksConfirmModel(query, linkSettings.UseItemParentId, linkSettings.SourceEntityType, linkSettings.DestinationEntityType);
    }

    /// <summary>
    /// Create the query to find what item links to delete for multiple columns.
    /// </summary>
    /// <param name="fileLines"></param>
    /// <param name="deleteLinksRequest">The criteria for the item links to delete.</param>
    /// <returns>Returns a collection of <see cref="DeleteLinksConfirmModel"/> containing the information to delete the links.</returns>
    private async Task<List<DeleteLinksConfirmModel>> CreateQueryForMultipleColumns(IList<string> fileLines, DeleteLinksRequestModel deleteLinksRequest)
    {
        var firstEntity = deleteLinksRequest.DeleteSettings[0]["entity"].ToString();
        var secondEntity = deleteLinksRequest.DeleteSettings[1]["entity"].ToString();

        //Get all link settings and select those where first and second entity are matching in either direction.
        IEnumerable<LinkSettingsModel> allLinkSettings = await wiserItemsService.GetAllLinkTypeSettingsAsync();
        allLinkSettings = allLinkSettings.Where(t => (String.Equals(t.SourceEntityType, firstEntity, StringComparison.OrdinalIgnoreCase) && String.Equals(t.DestinationEntityType, secondEntity, StringComparison.OrdinalIgnoreCase))
                                                     || (String.Equals(t.SourceEntityType, secondEntity, StringComparison.OrdinalIgnoreCase) && String.Equals(t.DestinationEntityType, firstEntity, StringComparison.OrdinalIgnoreCase)));

        var results = new List<DeleteLinksConfirmModel>();

        foreach (var linkSettings in allLinkSettings)
        {
            int sourceIndex;
            int destinationIndex;

            //Match provided information with the link settings to determine the source and destination.
            if (String.Equals(linkSettings.SourceEntityType, firstEntity, StringComparison.OrdinalIgnoreCase))
            {
                sourceIndex = 0;
                destinationIndex = 1;
            }
            else
            {
                sourceIndex = 1;
                destinationIndex = 0;
            }

            var sourceMatchTo = deleteLinksRequest.DeleteSettings[sourceIndex]["matchTo"].ToString();
            var destinationMatchTo = deleteLinksRequest.DeleteSettings[destinationIndex]["matchTo"].ToString();

            var sourceIsId = sourceMatchTo == "id";
            var destinationIsId = destinationMatchTo == "id";

            var sourceValues = new List<string>();
            var destinationValues = new List<string>();

            foreach (var line in fileLines)
            {
                var split = line.Split(';');
                if (!String.IsNullOrWhiteSpace(split[sourceIndex]))
                {
                    sourceValues.Add(split[sourceIndex]);
                }

                if (!String.IsNullOrWhiteSpace(split[destinationIndex]))
                {
                    destinationValues.Add(split[destinationIndex]);
                }
            }

            var sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSettings.SourceEntityType);
            var destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkSettings.DestinationEntityType);
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(0, linkSettings.SourceEntityType, linkSettings.DestinationEntityType);

            clientDatabaseConnection.AddParameter("linkType", linkSettings.Type);
            clientDatabaseConnection.AddParameter("sourceEntity", linkSettings.SourceEntityType);
            clientDatabaseConnection.AddParameter("destinationEntity", linkSettings.DestinationEntityType);
            clientDatabaseConnection.AddParameter("sourceMatchTo", sourceMatchTo);
            clientDatabaseConnection.AddParameter("destinationMatchTo", destinationMatchTo);

            var query = new StringBuilder();

            if (linkSettings.UseItemParentId)
            {
                query.AppendLine($"""
                              SELECT source.id AS id, source.id AS sourceId, destination.id AS destinationId
                              FROM {sourceTablePrefix}{WiserTableNames.WiserItem} AS source
                              JOIN {destinationTablePrefix}{WiserTableNames.WiserItem} AS destination ON destination.id = source.parent_item_id AND destination.entity_type = ?destinationEntity
                              """);
                // If the source is a property join tables.
                if (!sourceIsId)
                {
                    query.AppendLine($"JOIN {sourceTablePrefix}{WiserTableNames.WiserItemDetail} AS sourceDetail ON sourceDetail.item_id = source.id AND sourceDetail.`key` = ?sourceMatchTo");
                }

                // If the destination is a property join tables.
                if (!destinationIsId)
                {
                    query.AppendLine($"JOIN {destinationTablePrefix}{WiserTableNames.WiserItemDetail} AS destinationDetail ON destinationDetail.item_id = destination.id AND destinationDetail.`key` = ?destinationMatchTo");
                }

                query.AppendLine("WHERE source.entity_type = ?sourceEntity");

                // Add WHERE statement for the source based on if it is an id or a property.
                if (sourceIsId)
                {
                    query.AppendLine($"AND source.id IN({String.Join(",", sourceValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
                else
                {
                    query.AppendLine($"AND sourceDetail.`value` IN({String.Join(",", sourceValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }

                // Add WHERE statement for the destination based on if it is an id or a property.
                if (destinationIsId)
                {
                    query.AppendLine($"AND source.parent_item_id IN({String.Join(",", destinationValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
                else
                {
                    query.AppendLine($"AND destinationDetail.`value` IN({String.Join(",", destinationValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
            }
            else
            {
                query.AppendLine($"""
                                  SELECT itemLink.id AS id, itemLink.item_id AS sourceId, itemLink.destination_item_id AS destinationId
                                  FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} AS itemLink
                                  """);

                //If the source is a property join tables.
                if (!sourceIsId)
                {
                    query.AppendLine($"""
                                      JOIN {sourceTablePrefix}{WiserTableNames.WiserItem} AS source ON source.id = itemLink.item_id AND source.entity_type = ?sourceEntity
                                      JOIN {sourceTablePrefix}{WiserTableNames.WiserItemDetail} AS sourceDetail ON sourceDetail.item_id = source.id AND sourceDetail.`key` = ?sourceMatchTo
                                      """);
                }

                //If the destination is a property join tables.
                if (!destinationIsId)
                {
                    query.AppendLine($"""
                                      JOIN {destinationTablePrefix}{WiserTableNames.WiserItem} AS destination ON destination.id = itemLink.destination_item_id AND destination.entity_type = ?destinationEntity
                                      JOIN {destinationTablePrefix}{WiserTableNames.WiserItemDetail} AS destinationDetail ON destinationDetail.item_id = destination.id AND destinationDetail.`key` = ?destinationMatchTo
                                      """);
                }

                query.AppendLine("WHERE itemLink.type = ?linkType");

                //Add WHERE statement for the source based on if it is an id or a property.
                if (sourceIsId)
                {
                    query.AppendLine($"AND itemLink.item_id IN({String.Join(",", sourceValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
                else
                {
                    query.AppendLine($"AND sourceDetail.`value` IN({String.Join(",", sourceValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }

                //Add WHERE statement for the destination based on if it is an id or a property.
                if (destinationIsId)
                {
                    query.AppendLine($"AND itemLink.destination_item_id IN({String.Join(",", destinationValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
                else
                {
                    query.AppendLine($"AND destinationDetail.`value` IN({String.Join(",", destinationValues.Select(line => line.ToMySqlSafeValue(true)))})");
                }
            }

            results.Add(await CreateDeleteLinksConfirmModel(query.ToString(), linkSettings.UseItemParentId, linkSettings.SourceEntityType, linkSettings.DestinationEntityType));
        }

        return results;
    }

    /// <summary>
    /// Create a <see cref="DeleteLinksConfirmModel"/> with the given information.
    /// </summary>
    /// <param name="query">The query to perform.</param>
    /// <param name="useParentId">Whether the link is based on a parent id.</param>
    /// <param name="sourceEntityType">The type of the source entity.</param>
    /// <param name="destinationEntityType">The type of the destination entity.</param>
    /// <returns>Returns a <see cref="DeleteLinksConfirmModel"/> with the results.</returns>
    private async Task<DeleteLinksConfirmModel> CreateDeleteLinksConfirmModel(string query, bool useParentId, string sourceEntityType, string destinationEntityType)
    {
        var result = new DeleteLinksConfirmModel
        {
            Ids = [],
            UseParentId = useParentId,
            SourceEntityType = sourceEntityType,
            SourceIds = [],
            DestinationEntityType = destinationEntityType,
            DestinationIds = []
        };

        var dataTable = await clientDatabaseConnection.GetAsync(query);

        foreach (DataRow dataRow in dataTable.Rows)
        {
            result.Ids.Add(Convert.ToUInt64(dataRow["id"]));
            result.SourceIds.Add(Convert.ToUInt64(dataRow["sourceId"]));
            result.DestinationIds.Add(Convert.ToUInt64(dataRow["destinationId"]));
        }

        return result;
    }
}