using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Items.Models;
using Api.Modules.VersionControl.Enums;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace Api.Modules.VersionControl.Services;

/// <inheritdoc cref="IReviewService" />
public class ReviewService : IReviewService, IScopedService
{
    private readonly IReviewDataService reviewDataService;

    /// <summary>
    /// Creates a new instance of <see cref="ReviewService"/>.
    /// </summary>
    public ReviewService(IReviewDataService reviewDataService)
    {
        this.reviewDataService = reviewDataService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<ReviewModel>>> GetAsync(ClaimsIdentity identity, bool hideApprovedReviews = true, bool getReviewsForCurrentUserOnly = false)
    {
        var isAdmin = IdentityHelpers.IsAdminAccount(identity);
        var userId = isAdmin ? Convert.ToInt64(IdentityHelpers.GetWiserAdminId(identity)) * -1 : Convert.ToInt64(IdentityHelpers.GetWiserUserId(identity));

        var reviews = await reviewDataService.GetAsync(hideApprovedReviews, getReviewsForCurrentUserOnly ? userId : 0);

        return new ServiceResult<List<ReviewModel>>(reviews);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ReviewModel>> RequestReviewForCommitAsync(ClaimsIdentity identity, int commitId, List<FlatItemModel> requestedUsers)
    {
        var isAdmin = IdentityHelpers.IsAdminAccount(identity);

        var review = new ReviewModel
        {
            CommitId = commitId,
            RequestedBy = isAdmin ? Convert.ToInt64(IdentityHelpers.GetWiserAdminId(identity)) * -1 : Convert.ToInt64(IdentityHelpers.GetWiserUserId(identity)),
            RequestedByName = isAdmin ? IdentityHelpers.GetAdminUserName(identity) : IdentityHelpers.GetName(identity),
            RequestedUsers = requestedUsers.Select(user => Convert.ToInt64(user.Id) * (user.Fields.TryGetValue("isAdmin", out var value) && (bool)value ? -1 : 1)).ToList(),
            Status = ReviewStatuses.Pending
        };

        return new ServiceResult<ReviewModel>(await reviewDataService.SaveReviewAsync(review));
    }
}