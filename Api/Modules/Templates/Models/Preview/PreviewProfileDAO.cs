namespace Api.Modules.Templates.Models.Preview
{
    /// <summary>
    /// Data Access Object to get or set information about the PreviewProfile in the database
    /// </summary>
    public class PreviewProfileDao
    {
        /// <summary>
        /// Gets or sets the ID of the PreviewProfile object
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the PreviewProfile object
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the PreviewProfile object
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Gets or sets the raw variables of the PreviewProfile object
        /// </summary>
        public string RawVariables { get; set; }

        /// <summary>
        /// Constructor to make an PreviewProfileDAO object. This is the information that will be written to the database.
        /// </summary>
        /// <param name="id">the ID of the PreviewProfile object</param>
        /// <param name="name">the name of the PreviewProfile object</param>
        /// <param name="url">the URL of the PreviewProfile object</param>
        /// <param name="variables">the raw variables of the PreviewProfile object</param>
        public PreviewProfileDao (int id, string name, string url, string variables)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
            this.RawVariables = variables;
        }
    }
}
