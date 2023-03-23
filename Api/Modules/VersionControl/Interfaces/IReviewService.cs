using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Items.Models;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces;

/// <summary>
/// A service for handling code reviews on commits and getting data for reviews.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Request certain users to do a code review for a specific commit.
    /// </summary>
    /// <param name="identity">The authenticated user data.</param>
    /// <param name="commitId">The ID of the commit to request the review for.</param>
    /// <param name="requestedUsers">The list of users that are requested to do the code review.</param>
    /// <returns>A <see cref="ReviewModel"/> with the saved data for the code review.</returns>
    Task<ServiceResult<ReviewModel>> RequestReviewForCommitAsync(ClaimsIdentity identity, int commitId, List<FlatItemModel> requestedUsers);
}