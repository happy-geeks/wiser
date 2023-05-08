namespace Api.Modules.Files.Models
{
    /// <summary>
    /// A model for a Wiser file.
    /// </summary>
    public class FileModel
    {
        /// <summary>
        /// Gets or sets the item ID of the item that the file belongs to.
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the file.
        /// </summary>
        public int FileId { get; set; }

        /// <summary>
        /// Gets or sets the content type of the file.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the title/description of the file.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the URL to the file, if this is an external file that is not saved in our database.
        /// </summary>
        public string ContentUrl { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the corresponding item, if this is a file saved on an item.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the type of the corresponding link, if this is a file saved on a link.
        /// </summary>
        public int LinkType { get; set; }
        
        /// <summary>
        /// Gets or sets the object for storing extra data, such as alt texts in multiple languages for images.
        /// </summary>
        public FileExtraDataModel ExtraData { get; set; }
    }
}
