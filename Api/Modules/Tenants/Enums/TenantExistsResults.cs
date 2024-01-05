using System;

namespace Api.Modules.Tenants.Enums
{
    /// <summary>
    /// All possible results for the tenant exists check.
    /// </summary>
    [Flags]
    public enum TenantExistsResults
    {
        /// <summary>
        /// The name and sub domain are both still available.
        /// </summary>
        Available = 0,

        /// <summary>
        /// The name is already in use by another tenant.
        /// </summary>
        NameNotAvailable = 1,

        /// <summary>
        /// The sub domain is already in use by another tenant.
        /// </summary>
        SubDomainNotAvailable = 2
    }
}