using System.Collections.Generic;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.VersionControl.Models;

/// <summary>
/// A model used for deploying multiple commits to a specici environment.
/// </summary>
public class DeployCommitsRequestModel
{
    /// <summary>
    /// Gets or sets the environment to deploy to.
    /// Any 'lower' environments will be automatically committed. So if it's set to live, then it will also be committed to acceptance and test.
    /// </summary>
    public Environments Environment { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the commits to deploy.
    /// </summary>
    public List<int> CommitIds { get; set; } = new();
}