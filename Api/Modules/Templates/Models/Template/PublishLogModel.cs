namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// Model class to keep track of the changes on the publish environments.
    /// </summary>
    public class PublishLogModel
    {
        /// <summary>
        /// Gets or sets the ID of the PublishLog object
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The old version number of the version on the live environment
        /// </summary>
        public int OldLive { get; set; }
        
        /// <summary>
        /// The old version number of the version on the acceptance environment
        /// </summary>
        public int OldAccept { get; set; }
        
        /// <summary>
        /// The old version number of the version on the test environment
        /// </summary>
        public int OldTest { get; set; }

        /// <summary>
        /// The new version number of the version on the live environment
        /// </summary>
        public int NewLive { get; set; }
        
        /// <summary>
        /// The new version number of the version on the acceptance environment
        /// </summary>
        public int NewAccept { get; set; }
        
        /// <summary>
        /// The new version number of the version on the test environment
        /// </summary>
        public int NewTest { get; set; }

        /// <summary>
        /// Create a PublishLogModel with all values set to a base. Will set the old environments as new values.
        /// </summary>
        /// <param name="id">The id of the template</param>
        /// <param name="oldLive">The version number of the current version on the Live environment.</param>
        /// <param name="oldAccept">The version number of the current version on the Accept environment.</param>
        /// <param name="oldTest">The version number of the current version on the Test environment.</param>
        public PublishLogModel(int id, int oldLive, int oldAccept, int oldTest)
        {
            this.Id = id;
            this.OldLive = oldLive;
            this.NewLive = oldLive;
            this.OldAccept = oldAccept;
            this.NewAccept = oldAccept;
            this.OldTest = oldTest;
            this.NewTest = oldTest;
        }

        /// <summary>
        /// Create a publishLogModel with all values set.
        /// </summary>
        /// <param name="id">The id of the template</param>
        /// <param name="oldLive">The version number of the current version on the Live environment.</param>
        /// <param name="oldAccept">The version number of the current version on the Accept environment.</param>
        /// <param name="oldTest">The version number of the current version on the Test environment.</param>
        /// <param name="newLive">The version number that is being published on the Live environment.</param>
        /// <param name="newAccept">The version number that is being published on the Accept environment.</param>
        /// <param name="newTest">The version number that is being published on the Test environment.</param>
        public PublishLogModel(int id, int oldLive, int oldAccept, int oldTest, int newLive, int newAccept, int newTest)
        {
            this.Id = id;
            this.OldLive = oldLive;
            this.NewLive = newLive;
            this.OldAccept = oldAccept;
            this.NewAccept = newAccept;
            this.OldTest = oldTest;
            this.NewTest = newTest;
        }
    }
}
