using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class RevertHistoryModel
    {
        public int Version { get; set; }
        public List<string> RevertedProperties { get; set; }

        public int GetVersion()
        {
            return Version;
        }

        public int GetVersionForRevision()
        {
            return Version - 1;
        }

        public List<string> GetRevertedProperties()
        {
            return RevertedProperties;
        }
    }
}
