using System.Collections.Generic;

namespace Api.Modules.Imports.Models
{
    //TODO Verify comments
    /// <summary>
    /// A model for the result of the Wiser import module.
    /// </summary>
    public class ImportResultModel
    {
        public uint ItemsTotal { get; set; }

        public uint ItemsCreated { get; set; }

        public uint ItemsUpdated { get; set; }

        public uint Successful { get; set; }

        public uint Failed { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> UserFriendlyErrors { get; set; } = new List<string>();
    }
}
