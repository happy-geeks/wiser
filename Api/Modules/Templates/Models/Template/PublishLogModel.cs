using System;

namespace Api.Modules.Templates.Models.Template
{
    public class PublishLogModel
    {
        public int Id { get; set; }

        public int OldLive { get; set; }
        public int OldAccept { get; set; }
        public int OldTest { get; set; }

        public int NewLive { get; set; }
        public int NewAccept { get; set; }
        public int NewTest { get; set; }

        public PublishLogModel()
        {

        }

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
