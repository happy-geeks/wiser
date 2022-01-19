using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Other
{
    public class PublishedEnvironmentModel
    {
        public int liveVersion { get; set; }
        public int acceptVersion { get; set; }
        public int testVersion { get; set; }
        public List<int> versionList { get; set; }

        public PublishedEnvironmentModel (int liveVersion, int acceptVersion, int testVersion, List<int> versionList)
        {
            this.liveVersion = liveVersion;
            this.acceptVersion = acceptVersion;
            this.testVersion = testVersion;
            this.versionList = versionList;
        }

        public List<int> GetVersionList() {
            return this.versionList;
        }

        public int GetLive()
        {
            return this.liveVersion;
        }
        public int GetAcceptance()
        {
            return this.acceptVersion;
        }
        public int GetTest()
        {
            return this.testVersion;
        }
    }
}
