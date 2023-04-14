using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Enums;
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
    private readonly ICommitDataService commitDataService;

    /// <summary>
    /// Creates a new instance of <see cref="ReviewDataService"/>.
    /// </summary>
    public ReviewDataService(IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService, ICommitDataService commitDataService)
    {
        this.databaseConnection = databaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.commitDataService = commitDataService;
    }

    /// <inheritdoc />
    public async Task<List<ReviewModel>> GetAsync(bool hideApprovedReviews = true, long userId = 0)
    {
        // Make sure the tables are up-to-date.
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommitReviews,
            WiserTableNames.WiserCommitReviewRequests,
            WiserTableNames.WiserCommitReviewComments
        });

        // Get all reviews.
        var whereClause = new List<string> { "TRUE" };
        var extraJoins = new List<string>();
        if (hideApprovedReviews)
        {
            whereClause.Add($"review.status != '{ReviewStatuses.Approved.ToString()}'");
        }

        if (userId != 0)
        {
            databaseConnection.AddParameter("userId", userId);
            extraJoins.Add($"JOIN {WiserTableNames.WiserCommitReviewRequests} AS requestedUserFilter ON requestedUserFilter.review_id = review.id AND requestedUserFilter.requested_user = ?userId");
        }

        var query = $@"SELECT
    review.id,
    review.commit_id,
    review.requested_on,
    review.requested_by,
    review.requested_by_name,
    review.reviewed_on,
    review.reviewed_by,
    review.reviewed_by_name,
    review.status,
    commit.id AS commit_id,
    commit.description AS commit_description,
    requestedUser.requested_user,
    comment.id AS comment_id,
    comment.added_on AS comment_added_on,
    comment.added_by AS comment_added_by,
    comment.added_by_name AS comment_added_by_name,
    comment.text AS comment_text
FROM {WiserTableNames.WiserCommitReviews} AS review
JOIN {WiserTableNames.WiserCommit} AS commit ON commit.id = review.commit_id
{String.Join(Environment.NewLine, extraJoins)}
LEFT JOIN {WiserTableNames.WiserCommitReviewRequests} AS requestedUser ON requestedUser.review_id = review.id
LEFT JOIN {WiserTableNames.WiserCommitReviewComments} AS comment ON comment.review_id = review.id
WHERE {String.Join(" AND ", whereClause)}
ORDER BY review.id DESC, comment.id DESC";

        var dataTable = await databaseConnection.GetAsync(query);
        var results = new List<ReviewModel>();

        // Loop through the data table and create the review models.
        foreach (DataRow row in dataTable.Rows)
        {
            var id = row.Field<int>("id");
            var review = results.FirstOrDefault(r => r.Id == id);
            if (review == null)
            {
                review = new ReviewModel
                {
                    Id = id,
                    CommitId = row.Field<int>("commit_id"),
                    CommitDescription = row.Field<string>("commit_description"),
                    RequestedOn = row.Field<DateTime>("requested_on"),
                    RequestedBy = row.Field<long>("requested_by"),
                    RequestedByName = row.Field<string>("requested_by_name"),
                    ReviewedOn = row.Field<DateTime?>("reviewed_on"),
                    ReviewedBy = row.Field<long?>("reviewed_by") ?? 0,
                    ReviewedByName = row.Field<string>("reviewed_by_name"),
                    Status = (ReviewStatuses)Enum.Parse(typeof(ReviewStatuses), row.Field<string>("status")),
                    RequestedUsers = new List<long>(),
                    Comments = new List<ReviewCommentModel>()
                };

                review.Commit = await commitDataService.GetCommitAsync(review.CommitId);

                results.Add(review);
            }

            if (!row.IsNull("requested_user"))
            {
                var requestedUser = row.Field<long>("requested_user");
                if (!review.RequestedUsers.Contains(requestedUser))
                {
                    review.RequestedUsers.Add(requestedUser);
                }
            }

            if (!row.IsNull("comment_id"))
            {
                review.Comments.Add(new ReviewCommentModel
                {
                    ReviewId = review.Id,
                    Id = row.Field<int>("comment_id"),
                    AddedOn = row.Field<DateTime>("comment_added_on"),
                    AddedBy = row.Field<long>("comment_added_by"),
                    AddedByName = row.Field<string>("comment_added_by_name"),
                    Text = row.Field<string>("comment_text")
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ReviewModel> GetAsync(int id, bool includeComments = true)
    {
        // Make sure the tables are up-to-date.
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommitReviews
        });

        databaseConnection.AddParameter("id", id);

        var commentsJoin = includeComments ? $"LEFT JOIN {WiserTableNames.WiserCommitReviewComments} AS comment ON comment.review_id = review.id" : "";
        var commentsSelect = includeComments ? ", comment.id AS comment_id, comment.added_on AS comment_added_on, comment.added_by AS comment_added_by, comment.added_by_name AS comment_added_by_name, comment.text AS comment_text" : "";
        var orderByClause = includeComments ? "ORDER BY review.id DESC, comment.id DESC" : "ORDER BY review.id DESC";

        var query = $@"SELECT
    review.id,
    review.commit_id,
    review.requested_on,
    review.requested_by,
    review.requested_by_name,
    review.reviewed_on,
    review.reviewed_by,
    review.reviewed_by_name,
    review.status,
    commit.id AS commit_id,
    commit.description AS commit_description,
    requestedUser.requested_user
    {commentsSelect}
FROM {WiserTableNames.WiserCommitReviews} AS review
JOIN {WiserTableNames.WiserCommit} AS commit ON commit.id = review.commit_id
LEFT JOIN {WiserTableNames.WiserCommitReviewRequests} AS requestedUser ON requestedUser.review_id = review.id
{commentsJoin}
WHERE review.id = ?id
{orderByClause}";

        var dataTable = await databaseConnection.GetAsync(query);
        var row = dataTable.Rows[0];
        var review = new ReviewModel
        {
            Id = id,
            CommitId = row.Field<int>("commit_id"),
            CommitDescription = row.Field<string>("commit_description"),
            RequestedOn = row.Field<DateTime>("requested_on"),
            RequestedBy = row.Field<long>("requested_by"),
            RequestedByName = row.Field<string>("requested_by_name"),
            ReviewedOn = row.Field<DateTime?>("reviewed_on"),
            ReviewedBy = row.Field<long?>("reviewed_by") ?? 0,
            ReviewedByName = row.Field<string>("reviewed_by_name"),
            Status = (ReviewStatuses)Enum.Parse(typeof(ReviewStatuses), row.Field<string>("status")),
            RequestedUsers = new List<long>(),
            Comments = new List<ReviewCommentModel>()
        };

        review.Commit = await commitDataService.GetCommitAsync(review.CommitId);

        // Loop through the data table and add requested users and comments.
        foreach (DataRow dataRow in dataTable.Rows)
        {
            if (!dataRow.IsNull("requested_user"))
            {
                var requestedUser = dataRow.Field<long>("requested_user");
                if (!review.RequestedUsers.Contains(requestedUser))
                {
                    review.RequestedUsers.Add(requestedUser);
                }
            }

            if (includeComments && !dataRow.IsNull("comment_id"))
            {
                review.Comments.Add(new ReviewCommentModel
                {
                    ReviewId = review.Id,
                    Id = dataRow.Field<int>("comment_id"),
                    AddedOn = dataRow.Field<DateTime>("comment_added_on"),
                    AddedBy = dataRow.Field<long>("comment_added_by"),
                    AddedByName = dataRow.Field<string>("comment_added_by_name"),
                    Text = dataRow.Field<string>("comment_text")
                });
            }
        }

        return review;
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

    /// <inheritdoc />
    public async Task AddOrUpdateCommentAsync(ReviewCommentModel comment)
    {
        if (String.IsNullOrWhiteSpace(comment?.Text))
        {
            throw new ArgumentNullException("comment.Text");
        }

        // Make sure the tables are up-to-date.
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommitReviewComments
        });

        // Save the comment data in the database.
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("review_id", comment.ReviewId);
        databaseConnection.AddParameter("added_on", comment.AddedOn);
        databaseConnection.AddParameter("added_by", comment.AddedBy);
        databaseConnection.AddParameter("added_by_name", comment.AddedByName);
        databaseConnection.AddParameter("text", comment.Text);

        await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserCommitReviewComments, comment.Id);
    }
}