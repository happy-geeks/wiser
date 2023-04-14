using System;
using System.Collections.Generic;
using Api.Modules.VersionControl.Enums;

namespace Api.Modules.VersionControl.Models;

/// <summary>
/// A model for storing data about a code review for the version control and templates modules.
/// </summary>
public class ReviewModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the review was requested.
    /// </summary>
    public DateTime RequestedOn { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user that requested this review.
    /// Note: Users with a negative ID are admins from the main Wiser database, others are normal users from the tenant.
    /// </summary>
    public long RequestedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that requested this review.
    /// </summary>
    public string RequestedByName { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the commit has been reviewed.
    /// </summary>
    public DateTime? ReviewedOn { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user that did the review.
    /// Note: Users with a negative ID are admins from the main Wiser database, others are normal users from the tenant.
    /// </summary>
    public long ReviewedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that did the review.
    /// </summary>
    public string ReviewedByName { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public ReviewStatuses Status { get; set; } = ReviewStatuses.None;

    /// <summary>
    /// Gets or sets the users that have been requested to do the code review.
    /// Note: Users with a negative ID are admins from the main Wiser database, others are normal users from the tenant.
    /// </summary>
    public List<long> RequestedUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the comments that have been placed on this review.
    /// </summary>
    public List<ReviewCommentModel> Comments { get; set; } = new();

    /// <summary>
    /// Gets or sets the ID of the commit that this review is meant for.
    /// </summary>
    public int CommitId { get; set; }

    /// <summary>
    /// Gets or sets the commit that this review is meant for.
    /// </summary>
    public CommitModel Commit { get; set; }

    /// <summary>
    /// Gets or sets the description of the commit.
    /// </summary>
    public string CommitDescription { get; set; }
}