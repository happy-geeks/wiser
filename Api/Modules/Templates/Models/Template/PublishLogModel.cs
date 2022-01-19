using System;

namespace Api.Modules.Templates.Models.Template
{
    public class PublishLogModel
    {
        public Int64 Id { get; set; }

        public Int64 oldLive { get; set; }
        public Int64 oldAccept { get; set; }
        public Int64 oldTest { get; set; }

        public Int64 newLive { get; set; }
        public Int64 newAccept { get; set; }
        public Int64 newTest { get; set; }

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
            this.oldLive = oldLive;
            this.newLive = oldLive;
            this.oldAccept = oldAccept;
            this.newAccept = oldAccept;
            this.oldTest = oldTest;
            this.newTest = oldTest;
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
            this.oldLive = oldLive;
            this.newLive = newLive;
            this.oldAccept = oldAccept;
            this.newAccept = newAccept;
            this.oldTest = oldTest;
            this.newTest = newTest;
        }
    }
}
