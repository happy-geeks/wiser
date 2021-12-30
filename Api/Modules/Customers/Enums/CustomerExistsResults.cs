using System;

namespace Api.Modules.Customers.Enums
{
    /// <summary>
    /// All possible results for the customer exists check.
    /// </summary>
    [Flags]
    public enum CustomerExistsResults
    {
        /// <summary>
        /// The name and sub domain are both still available.
        /// </summary>
        Available = 0,

        /// <summary>
        /// The name is already in use by another customer.
        /// </summary>
        NameNotAvailable = 1,

        /// <summary>
        /// The sub domain is already in use by another customer.
        /// </summary>
        SubDomainNotAvailable = 2
    }
}