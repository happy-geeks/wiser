using System;

namespace Api.Modules.Google.Exceptions;

/// <inheritdoc />
public class InvalidApiKeyException : Exception
{
    /// <inheritdoc />
    public InvalidApiKeyException(string message) : base(message)
    {
    }
    
}