using System;

namespace Api.Modules.Google.Exceptions;

public class InvalidApiKeyException : Exception
{
    public InvalidApiKeyException()
    {
    }

    public InvalidApiKeyException(string message) : base(message)
    {
    }

    public InvalidApiKeyException(string message, Exception inner) : base(message, inner)
    {
    }
}