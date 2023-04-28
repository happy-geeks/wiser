using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Files.Interfaces.Repository;
using Api.Modules.Files.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Files.Services.Repository;

/// <inheritdoc cref="IFilesRepository" />
public class FilesRepository : IFilesRepository, ITransientService
{
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of the <see cref="FilesRepository"/> class.
    /// </summary>
    public FilesRepository(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<List<FileTreeViewModel>> GetTreeAsync(ulong parentId = 0, string tablePrefix = "")
    {
        await databaseConnection.EnsureOpenConnectionForReadingAsync();
        databaseConnection.AddParameter("parentId", parentId);

        var query = $@"SELECT
    id,
    file_name AS name,
    content_type AS contentType,
    FALSE AS isDirectory,
    FALSE AS hasChildren,
    property_name AS propertyName,
    item_id AS itemId,
    CASE
        WHEN content_type LIKE 'image/%' THEN 'image'
        WHEN content_type = 'text/html' THEN 'html'
        ELSE 'file'
    END AS spriteCssClass,
    IF(content_type IN('text/html', 'application/octet-stream'), CONVERT(content USING utf8), '') AS html
FROM {tablePrefix}{WiserTableNames.WiserItemFile}
WHERE item_id = ?parentId

UNION ALL

SELECT
    item.id,
    item.title AS name,
    '' AS contentType,
    TRUE AS isDirectory,
    IF(COUNT(DISTINCT subItem.id) > 0 OR COUNT(DISTINCT file.id) > 0, TRUE, FALSE) AS hasChildren,
    '' AS propertyName,
    item.id AS itemId,
    '{Constants.ClosedDirectoryIconClass}' AS spriteCssClass,
    '' AS html
FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
LEFT JOIN {tablePrefix}{WiserTableNames.WiserItem} AS subItem ON subItem.entity_type = '{Constants.FilesDirectoryEntityType}' AND subItem.parent_item_id = item.id
LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemFile} AS file ON file.item_id = item.id
WHERE item.entity_type = '{Constants.FilesDirectoryEntityType}'
AND item.parent_item_id = ?parentId
GROUP BY item.id

ORDER BY isDirectory DESC, name ASC";

        var dataTable = await databaseConnection.GetAsync(query);
        return dataTable.Rows.Cast<DataRow>().Select(dataRow =>
        {
            var model = new FileTreeViewModel();
            model.Id = Convert.ToUInt64(dataRow["id"]);
            model.Name = dataRow.Field<string>("name");
            model.IsDirectory = Convert.ToBoolean(dataRow["isDirectory"]);
            model.HasChildren = Convert.ToBoolean(dataRow["hasChildren"]);
            model.CollapsedSpriteCssClass = dataRow.Field<string>("spriteCssClass");
            model.ExpandedSpriteCssClass = Constants.OpenedDirectoryIconClass;
            model.Html = dataRow.Field<string>("html");
            model.PropertyName = dataRow.Field<string>("propertyName");
            model.ItemId = dataRow.Field<ulong>("itemId");
            model.ContentType = dataRow.Field<string>("contentType");
            return model;
        }).ToList();
    }
}