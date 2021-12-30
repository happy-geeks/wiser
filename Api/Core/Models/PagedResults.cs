using System.Collections.Generic;

namespace Api.Core.Models
{
    /// <summary>
    /// A model for returning paged results.
    /// </summary>
    /// <typeparam name="T">The type of result to return.</typeparam>
    public class PagedResults<T>
    {
        /// <summary>
        /// The page number this page represents. 
        /// </summary>
        public int PageNumber { get; set; }
    
        /// <summary> 
        /// The size of this page. 
        /// </summary> 
        public int PageSize { get; set; }
    
        /// <summary> 
        /// The total number of pages available. 
        /// </summary> 
        public int TotalNumberOfPages { get; set; }
    
        /// <summary> 
        /// The total number of records available. 
        /// </summary> 
        public int TotalNumberOfRecords { get; set; }
    
        /// <summary> 
        /// The URL to the next page - if null, there are no more pages. 
        /// </summary> 
        public string PreviousPageUrl { get; set; } 
    
        /// <summary> 
        /// The URL to the next page - if null, there are no more pages. 
        /// </summary> 
        public string NextPageUrl { get; set; }

        /// <summary> 
        /// The records this page represents. 
        /// </summary> 
        public IEnumerable<T> Results { get; set; } = new List<T>();
    }
}