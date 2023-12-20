namespace Api.Modules.Templates.Enums
{
    /// <summary>
    /// The options for log booleans, this includes inherit
    /// </summary>
    public enum LogBoolean
    {
        inherit,
        /// <summary>
        /// true and false are reserved keywords in C#, so we use @ instead
        /// </summary>
        @true,
        @false
    }
}