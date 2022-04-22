namespace Api.Modules.EntityProperties.Enums
{
    /// <summary>
    /// An enum containing all possible label styles for fields.
    /// </summary>
    public enum EntityPropertyLabelStyles
    {
        /// <summary>
        /// The label is shown above the field.
        /// </summary>
        Normal,
        
        /// <summary>
        /// The label is shown left of the field.
        /// </summary>
        Inline,
        
        /// <summary>
        /// The label will be shown inside the field as a placeholder while the field is empty and will be moved to above the field when it does contain a value.
        /// </summary>
        Float
    }
}
