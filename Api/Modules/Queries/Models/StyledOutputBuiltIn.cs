namespace Api.Modules.Queries.Models;

/// <summary>
/// A model for a StyledOutputBuildIn within StyledOutput used for storing build in query/keyword and layout combinations.
/// </summary>
public class StyledOutputBuiltIn
{
    /// <summary>
    /// The key used to identify the 'command' in a styled output.
    /// </summary>
    public string Key;

    /// <summary>
    /// The query that needs to be ran to gather the needed deta.
    /// </summary>
    public string Query;

    /// <summary>
    /// Layout that gets placed in the front of a result set if this is required by the function handling it.
    /// </summary>
    public string BeginLayout = "";

    /// <summary>
    /// Layout that gets placed at the end of a result set if this is required by the function handling it.
    /// </summary>
    public string EndLayout = "";

    /// <summary>
    /// The Layout that is used to format the output of the query result.
    /// </summary>
    public string UnitLayout;
}