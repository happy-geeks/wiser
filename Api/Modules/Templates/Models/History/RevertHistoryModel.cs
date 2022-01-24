using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class RevertHistoryModel
    {
        public int Version { get; set; }
        public List<string> RevertedProperties { get; set; }
        
        public int GetVersionForRevision()
        {
            return Version - 1;
        }
    }
}
