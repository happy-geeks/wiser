using System.Collections.Generic;

namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A model for the result for synchronising changes from an environment to the production environment of a tenant/tenant.
    /// </summary>
    public class MergeChangesToMainBranchResultModel
    {
        /// <summary>
        /// Gets or sets the amount of successfully synchronised changes.
        /// </summary>
        public uint SuccessfulChanges { get; set; }

        /// <summary>
        /// Gets or sets the list of errors that occurred.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}