using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services.DataLayer;

/// <inheritdoc cref="IReviewDataService" />
public class ReviewDataService : IReviewDataService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;
    private readonly IDatabaseHelpersService databaseHelpersService;

    /// <summary>
    /// Creates a new instance of <see cref="ReviewDataService"/>.
    /// </summary>
    public ReviewDataService(IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService)
    {
        this.databaseConnection = databaseConnection;
        this.databaseHelpersService = databaseHelpersService;
    }

    /// <inheritdoc />
    public async Task<ReviewModel> SaveReviewAsync(ReviewModel review)
    {
        // Make sure the tables are up-to-date.
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommitReviews,
            WiserTableNames.WiserCommitReviewRequests,
            WiserTableNames.WiserCommitReviewComments
        });

        // Save the review data in the database.
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("commit_id", review.CommitId);
        databaseConnection.AddParameter("requested_on", review.RequestedOn);
        databaseConnection.AddParameter("requested_by", review.RequestedBy);
        databaseConnection.AddParameter("requested_by_name", review.RequestedByName);
        databaseConnection.AddParameter("status", review.Status.ToString());

        if (review.ReviewedOn.HasValue)
        {
            databaseConnection.AddParameter("reviewed_on", review.ReviewedOn);
            databaseConnection.AddParameter("reviewed_by", review.ReviewedBy);
            databaseConnection.AddParameter("reviewed_by_name", review.ReviewedByName);
        }

        review.Id = await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserCommitReviews, review.Id);

        if (review.RequestedUsers == null || !review.RequestedUsers.Any())
        {
            return review;
        }

        // Link requested users to the review.
        var query = $@"INSERT IGNORE INTO {WiserTableNames.WiserCommitReviewRequests} (review_id, requested_user)
VALUES {String.Join(", ", review.RequestedUsers.Select(userId => $"({review.Id}, {userId})"))}";
        await databaseConnection.ExecuteAsync(query);

        return review;
    }
}