using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Other
{
    public class PublishedEnvironmentModel
    {
        public int LiveVersion { get; set; }
        public int AcceptVersion { get; set; }
        public int TestVersion { get; set; }
        public List<int> VersionList { get; set; }
    }
}
