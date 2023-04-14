using System;
using System.Collections.Generic;
using System.Linq;
using Api.Modules.Items.Models;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.VersionControl.Models;

/// <summary>
/// A model used for creating a new commit.
/// </summary>
public class CommitModel
{
    /// <summary>
    /// Gets or sets the id of the commit.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the commit.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the external ID. This can be used to link a commit to a task/project from an external system, such as Jira or Asana.
    /// </summary>
    public string ExternalId { get; set; }

    /// <summary>
    /// Gets or sets the time that the commit was added.
    /// </summary>
    public DateTime AddedOn { get; set; }

    /// <summary>
    /// Gets or sets the user that added the commit.
    /// </summary>
    public string AddedBy { get; set; }

    /// <summary>
    /// Gets or sets the templates that are part of this commit.
    /// </summary>
    public List<TemplateCommitModel> Templates { get; set; } = new();

    /// <summary>
    /// Gets or sets the dynamic contents that are part of this commit.
    /// </summary>
    public List<DynamicContentCommitModel> DynamicContents { get; set; } = new();

    /// <summary>
    /// Gets or sets the environment to commit this.
    /// Any 'lower' environments will be automatically committed. So if it's set to live, then it will also be committed to acceptance and test.
    /// </summary>
    public Environments Environment { get; set; }

    /// <summary>
    /// Gets or sets the time that this commit was deployed to development.
    /// </summary>
    public DateTime? DeployedToDevelopmentOn { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that deployed this commit to development.
    /// </summary>
    public string DeployedToDevelopmentBy { get; set; }

    /// <summary>
    /// Gets or sets the time that this commit was deployed to test.
    /// </summary>
    public DateTime? DeployedToTestOn { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that deployed this commit to test.
    /// </summary>
    public string DeployedToTestBy { get; set; }

    /// <summary>
    /// Gets or sets the time that this commit was deployed to acceptance.
    /// </summary>
    public DateTime? DeployedToAcceptanceOn { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that deployed this commit to acceptance.
    /// </summary>
    public string DeployedToAcceptanceBy { get; set; }

    /// <summary>
    /// Gets or sets the time that this commit was deployed to live.
    /// </summary>
    public DateTime? DeployedToLiveOn { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that deployed this commit to live.
    /// </summary>
    public string DeployedToLiveBy { get; set; }

    /// <summary>
    /// Gets or sets whether the commit has been completed.
    /// </summary>
    public bool Completed { get; set; }

    /// <summary>
    /// Gets whether this commit has been fully deployed to test.
    /// </summary>
    public bool IsTest => (!Templates.Any() || Templates.All(t => t.IsTest)) && (!DynamicContents.Any() || DynamicContents.All(d => d.IsTest));

    /// <summary>
    /// Gets whether this commit has been fully deployed to test.
    /// </summary>
    public bool IsAcceptance => (!Templates.Any() || Templates.All(t => t.IsAcceptance)) && (!DynamicContents.Any() || DynamicContents.All(d => d.IsAcceptance));

    /// <summary>
    /// Gets whether this commit has been fully deployed to test.
    /// </summary>
    public bool IsLive => (!Templates.Any() || Templates.All(t => t.IsLive)) && (!DynamicContents.Any() || DynamicContents.All(d => d.IsLive));

    /// <summary>
    /// Gets or sets the users that have been requested to do a code review for this commit.
    /// Note: Users with a negative ID are admins from the main Wiser database, others are normal users from the tenant.
    /// </summary>
    public List<FlatItemModel> ReviewRequestedUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the review status.
    /// </summary>
    public ReviewModel Review { get; set; }
}