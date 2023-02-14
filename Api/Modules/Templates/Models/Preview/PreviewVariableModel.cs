namespace Api.Modules.Templates.Models.Preview
{
    /// <summary>
    /// A model to store information about a preview variable for a template or component.
    /// </summary>
    public class PreviewVariableModel
    {
        /// <summary>
        /// Gets or sets the type of variable in the PreviewVariable object
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the key of the variable in the PreviewVariable object
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// Gets or sets the value of the variable in the PreviewVariable object
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Gets or sets a boolean of the variable is encrypted or not
        /// </summary>
        public bool Encrypt { get; set; }
    }
}
