namespace Api.Modules.Files.Models;

/// <summary>
/// Constants for the files module.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The entity type that is used for directories of global files.
    /// </summary>
    public const string FilesDirectoryEntityType = "filedirectory";

    /// <summary>
    /// The class name to use for the icon of a closed directory.
    /// </summary>
    public const string ClosedDirectoryIconClass = "wiserfolderclosed";

    /// <summary>
    /// The class name to use for the icon of an opened directory.
    /// </summary>
    public const string OpenedDirectoryIconClass = "wiserfolderopened";

    /// <summary>
    /// The property name for a global file.
    /// </summary>
    public const string GlobalFilePropertyName = "global_file";
}