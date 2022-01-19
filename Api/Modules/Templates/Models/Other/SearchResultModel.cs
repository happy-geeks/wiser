using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Other
{
    public class SearchResultModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string parent { get; set; }

        public Dictionary<string, object> GetSearchMatch()
        {
            return null;
        }
    }
}
