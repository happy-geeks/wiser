using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Other
{
    /// <summary>
    /// Model class to get the versions of the different environments that can be published to.
    /// </summary>
    public class PublishedEnvironmentModel
    {
        /// <summary>
        /// Gets or sets the version number of the object running on the live environment
        /// </summary>
        public int LiveVersion { get; set; }
        
        /// <summary>
        /// Gets or sets the version number of the object running on the acceptance environment
        /// </summary>
        public int AcceptVersion { get; set; }
        
        /// <summary>
        /// Gets or sets the version number of the object running on the test environment
        /// </summary>
        public int TestVersion { get; set; }
        
        /// <summary>
        /// Gets or sets a list of versions of the object.
        /// </summary>
        public List<int> VersionList { get; set; }
    }
}
