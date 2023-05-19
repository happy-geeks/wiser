using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Files.Models;

namespace Api.Modules.Files.Interfaces.Repository;

/// <summary>
/// Service for doing CRUD operations on files in database.
/// </summary>
public interface IFilesRepository
{
    /// <summary>
    /// Gets all items in a tree view from a parent.
    /// </summary>
    /// <param name="parentId">Optional: The parent ID. If no value is given, then the items in the root will be retrieved.</param>
    /// <param name="tablePrefix">Optional: If the files and directories have their own dedicated tables for wiser_item and wiser_itemfile, enter the prefix for that here.</param>
    /// <returns>A list of <see cref="FileTreeViewModel"/>.</returns>
    Task<List<FileTreeViewModel>> GetTreeAsync(ulong parentId = 0, string tablePrefix = "");
}