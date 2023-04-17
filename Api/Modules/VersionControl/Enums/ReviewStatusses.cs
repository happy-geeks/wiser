namespace Api.Modules.VersionControl.Enums;

/// <summary>
/// An enumeration with all possible statuses for a code review.
/// </summary>
public enum ReviewStatuses
{
    /// <summary>
    /// No review has been requested.
    /// </summary>
    None,
    /// <summary>
    /// The changes are still waiting for someone to review it.
    /// </summary>
    Pending,
    /// <summary>
    /// The changes have been approved.
    /// </summary>
    Approved,
    /// <summary>
    /// The reviewer requested changes to be made.
    /// </summary>
    RequestChanges
}