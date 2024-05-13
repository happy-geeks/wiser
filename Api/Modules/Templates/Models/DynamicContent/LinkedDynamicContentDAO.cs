using System;

namespace Api.Modules.Templates.Models.DynamicContent
{

    /// <summary>
    /// Data Access Object for Linked Dynamic Content.
    /// </summary>
    public class LinkedDynamicContentDao
    {

        /// <summary>
        /// Gets or sets the ID of the Linked Dynamic Content.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the component of the Linked Dynamic Content.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the component mode of the Linked Dynamic Content.
        /// </summary>
        public string ComponentMode { get; set; }

        /// <summary>
        /// Gets or sets the usages of the Linked Dynamic Content DAO.
        /// </summary>
        public string Usages { get; set; }

        /// <summary>
        /// Gets or sets the date and time of when the Linked Dynamic Content was created.
        /// </summary>
        public DateTime AddedOn { get; set; }

        /// <summary>
        /// Gets or sets who created the Linked Dynamic Content.
        /// </summary>
        public string AddedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time of when the Linked Dynamic Content last changed.
        /// </summary>
        public DateTime ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets who last changed the Linked Dynamic Content.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// The title of the Linked Dynamic Content.
        /// </summary>
        public string Title { get; set; }
    }
}