using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer;

/// <summary>
/// Data service for handling code reviews on commits;
/// </summary>
public interface IReviewDataService
{
    /// <summary>
    /// Gets all reviews.
    /// </summary>
    /// <param name="hideApprovedReviews">Optional: Whether to only get reviews that haven't been approved yet. Default is true.</param>
    /// <param name="userId">Optional: If you only want reviews that are waiting on a specific user, enter the ID of that user here. Negative numbers for admin accounts.</param>
    /// <returns>A list with all (not approved) reviews.</returns>
    Task<List<ReviewModel>> GetAsync(bool hideApprovedReviews = true, long userId = 0);

    /// <summary>
    /// Saves a review to the database. This can create a new review request, or update an existing review with the a new status.
    /// </summary>
    /// <param name="review">The requested review.</param>
    /// <returns>The requested review with the new ID.</returns>
    Task<ReviewModel> SaveReviewAsync(ReviewModel review);
}