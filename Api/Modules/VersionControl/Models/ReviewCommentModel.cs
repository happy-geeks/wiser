using System;

namespace Api.Modules.VersionControl.Models;

/// <summary>
/// A model for storing data of a comment on a code review.
/// </summary>
public class ReviewCommentModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the review that this comment is placed on.
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the comment was placed.
    /// </summary>
    public DateTime AddedOn { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user that placed the comment.
    /// Note: Users with a negative ID are admins from the main Wiser database, others are normal users from the tenant.
    /// </summary>
    public long AddedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user that placed the comment.
    /// </summary>
    public string AddedByName { get; set; }

    /// <summary>
    /// Gets or sets the contents of the comment.
    /// </summary>
    public string Text { get; set; }
}