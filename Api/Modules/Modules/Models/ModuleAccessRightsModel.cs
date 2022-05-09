namespace Api.Modules.Modules.Models
{
    /// <summary>
    /// A model for storing what the authenticated user can do with a module.
    /// </summary>
    public class ModuleAccessRightsModel
    {
        /// <summary>
        /// Gets or sets the ID of the module.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the icon of the module.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the main color of the module.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the type of module.
        /// Most modules are of the type "DynamicItems", which is also the default value.
        /// Other modules could be "Admin" or "Scheduler" for example, which means they are very different than the standard dynamic modules.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to read items in the module.
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to change existing items in the module.
        /// </summary>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to create new items in the module.
        /// </summary>
        public bool CanCreate { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to delete items in the module.
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Gets or sets the group name of the module.
        /// </summary>
        public string Group { get; set; }
        
        /// <summary>
        /// Gets or sets if the module is pinned by the user.
        /// </summary>
        public bool Pinned { get; set; }

        /// <summary>
        /// Gets or sets whether the module should be automatically opened when the user logs in.
        /// </summary>
        public bool AutoLoad { get; set; }

        /// <summary>
        /// Gets or sets the group name of the module for when the module is pinned by the user.
        /// </summary>
        public string PinnedGroup { get; set; }

        /// <summary>
        /// Gets or sets whether the user can only open one instance of this module. If they already have this module opened and they try to open it again, then the previously opened instance will be shown to the user.
        /// If this is set to <see langword="false"/>, then the user can open as many instances of this module as they want.
        /// </summary>
        public bool OnlyOneInstanceAllowed { get; set; }

        /// <summary>
        /// Gets or sets the iframe URL. Only applicable for modules of type "Iframe".
        /// </summary>
        public string IframeUrl { get; set; }
    }
}
