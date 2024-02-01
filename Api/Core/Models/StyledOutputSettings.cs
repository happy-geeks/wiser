using System;
using System.Collections.Generic;
using Api.Modules.Tenants.Interfaces;

namespace Api.Core.Models
{
    /// <summary>
    /// A model with settings for the styled output API. This is meant to be used with the IOptions pattern.
    /// </summary>
    public class StyledOutputSettings
    {
        /// <summary>
        /// Gets or sets the default results per page number
        /// </summary>
        public int MaxResultsPerPage { get; set; } = 500;
    }
}
