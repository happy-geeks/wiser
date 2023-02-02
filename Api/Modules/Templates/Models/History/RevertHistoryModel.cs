using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    /// <summary>
    /// A model class to revert history changes
    /// </summary>
    public class RevertHistoryModel
    {
        /// <summary>
        /// Gets or sets the version number of the RevertHistory object
        /// </summary>
        public int Version { get; set; }
        
        /// <summary>
        /// Gets or sets a list of the properties that need to be reverted
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> RevertedProperties { get; set; }
        
        /// <summary>
        /// Gets the version number to be used for revision
        /// </summary>
        /// <returns>The current version minus one</returns>
        public int GetVersionForRevision()
        {
            return Version - 1;
        }
    }
}
