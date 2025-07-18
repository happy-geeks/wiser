namespace Api.Modules.Templates.Enums;

/// <summary>
/// The possible values for the OnlyWith property in the HttpApiModel.
/// </summary>
public enum OnlyWithTypes
{
    /// <summary>
    /// No filtering applied, always execute the action.
    /// </summary>
    None,

    /// <summary>
    /// Only execute if the API response has a specific status code.
    /// </summary>
    OnlyWithStatusCode,

    /// <summary>
    /// Only execute if the API response has a specific success state.
    /// </summary>
    OnlyWithSuccessState,

    /// <summary>
    /// Only execute if the API response has a specific value.
    /// </summary>
    OnlyWithValue
}