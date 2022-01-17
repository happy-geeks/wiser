namespace Api.Modules.Babel.Models
{
    /// <summary>
    /// A model for returning a Babel API response.
    /// </summary>
    public class ResponseModel
    {
        /// <summary>
        /// Gets or sets whether the Babel conversion was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result of the Babel conversion.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets any comments about the Babel conversion.
        /// </summary>
        public string Comment { get; set; }
    }
}
