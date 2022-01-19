using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class RevertHistoryModel
    {
        public int version { get; set; }
        public List<string> revertedProperties { get; set; }

        public int GetVersion()
        {
            return version;
        }

        public int GetVersionForRevision()
        {
            return version - 1;
        }

        public List<string> GetRevertedProperties()
        {
            return revertedProperties;
        }
    }
}
