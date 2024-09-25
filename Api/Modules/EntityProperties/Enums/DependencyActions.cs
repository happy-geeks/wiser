namespace Api.Modules.EntityProperties.Enums
{
    /// <summary>
    /// An enum containing all possible action for a dependency between two fields.
    /// </summary>
    public enum DependencyActions
    {
        /// <summary>
        /// Toggle the visibility of the field, based on the value of the dependant field.
        /// </summary>
        ToggleVisibility,

        /// <summary>
        /// Toggle the mandatory state of the field, based on the value of the dependant field.
        /// </summary>
        ToggleMandatory,

        /// <summary>
        /// Automatically refresh the field when the value of the dependant field gets changed.
        /// </summary>
        Refresh
    }
}
