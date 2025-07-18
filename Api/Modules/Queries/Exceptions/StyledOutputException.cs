using System;

namespace Api.Modules.Queries.Exceptions;

/// <summary>
/// Custom exception for issues with styled output.
/// </summary>
public class StyledOutputException : Exception
{
    /// <summary>
    /// Create a new <see cref="StyledOutputException"/> with only a message/
    /// </summary>
    /// <param name="message">The message of the error.</param>
    public StyledOutputException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a new <see cref="StyledOutputException"/> with a message and an inner exception to include an existing exception.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <param name="innerException">The inner exception to be included.</param>
    public StyledOutputException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var exceptionMessage = InnerException != null ? $"with the following message: {InnerException.Message}" : String.Empty;
        return $"{base.ToString()}{Environment.NewLine}{Environment.NewLine}StyledOutput Failed {exceptionMessage}";
    }
}