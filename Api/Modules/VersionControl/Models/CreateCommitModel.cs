﻿using System;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model used for creating a new commit.
    /// </summary>
    public class CreateCommitModel
    {
        /// <summary>
        /// The id of the commit.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The description of the commit.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The Asana id.
        /// </summary>
        public int AsanaId { get; set; }

        /// <summary>
        /// The time The commit was added.
        /// </summary>
        public DateTime AddedOn { get; set; }

        /// <summary>
        /// The user the commit was changed by.
        /// </summary>
        public string ChangedBy { get; set; }
    }
}
