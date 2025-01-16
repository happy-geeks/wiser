using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Branches.Models;

namespace Api.Modules.Branches.Models;

/// <summary>
/// A model to return the result of setting up a merge of a branch.
/// </summary>
public class MergeBranchResultModel
{
    /// <summary>
    /// Gets or sets whether the merge has been added successfully to the queue.
    /// If this is false, it will mean that there are some conflicts that the user has to resolve first.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the list of conflicts.
    /// </summary>
    public List<MergeConflictModel> Conflicts { get; set; }
}