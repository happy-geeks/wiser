namespace Api.Core.Models
{
    /// <summary>
    /// A model for a request to the API for a single page.
    /// </summary>
    public class PagedRequest
    {
        /// <summary>
        /// Gets or sets the page number to get.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Gets or sets the amount of items per page.
        /// </summary>
        public int PageSize { get; set; } = 100;
    }
}