using System;

namespace Api.Modules.Templates.Models.Template
{
    public class PublishLogModel
    {
        public Int64 Id { get; set; }

        public Int64 OldLive { get; set; }
        public Int64 OldAccept { get; set; }
        public Int64 OldTest { get; set; }

        public Int64 NewLive { get; set; }
        public Int64 NewAccept { get; set; }
        public Int64 NewTest { get; set; }

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
        public PublishLogModel(Int64 id, Int64 oldLive, Int64 oldAccept, Int64 oldTest)
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
        public PublishLogModel(Int64 id, Int64 oldLive, Int64 oldAccept, Int64 oldTest, Int64 newLive, Int64 newAccept, Int64 newTest)
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
