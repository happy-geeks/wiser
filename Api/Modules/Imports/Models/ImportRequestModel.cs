using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Imports.Models
{
    /// <summary>
    /// A model for a request to add a new import to the queue.
    /// </summary>
    public class ImportRequestModel
    {
        /// <summary>
        /// Gets or sets the path for the uploaded file.
        /// </summary>
        [Required]
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the settings of the import.
        /// </summary>
        [Required]
        public List<Dictionary<string, object>> ImportSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings of the link import.
        /// </summary>
        public List<Dictionary<string, object>> ImportLinkSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings of the link detail import.
        /// </summary>
        public List<Dictionary<string, object>> ImportLinkDetailSettings { get; set; }

        /// <summary>
        /// Gets or sets the image file names.
        /// </summary>
        public string ImagesFileName { get; set; }

        /// <summary>
        /// Gets or sets the path for the uploaded images.
        /// </summary>
        public string ImagesFilePath { get; set; }

        /// <summary>
        /// Gets or sets the email address to mail when the import finished.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the import.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the import needs to start.
        /// </summary>
        public DateTime? StartDate { get; set; }
    }
}
