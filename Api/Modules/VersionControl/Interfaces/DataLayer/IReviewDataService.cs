using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer;

/// <summary>
/// Data service for handling code reviews on commits;
/// </summary>
public interface IReviewDataService
{
    /// <summary>
    /// Saves a review to the database. This can create a new review request, or update an existing review with the a new status.
    /// </summary>
    /// <param name="review">The requested review.</param>
    /// <returns>The requested review with the new ID.</returns>
    Task<ReviewModel> SaveReviewAsync(ReviewModel review);
}