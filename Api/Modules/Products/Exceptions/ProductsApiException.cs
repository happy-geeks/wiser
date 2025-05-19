using System;

namespace Api.Modules.Products.Exceptions;

/// <summary>
/// Custom exception for issues with the products api.
/// </summary>
public class ProductsApiException : Exception
{
    /// <summary>
    /// Create a new <see cref="ProductsApiException"/> with only a message/
    /// </summary>
    /// <param name="message">The message of the error.</param>
    public ProductsApiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a new <see cref="ProductsApiException"/> with a message and an inner exception to include an existing exception.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <param name="innerException">The inner exception to be included.</param>
    public ProductsApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{base.ToString()}{Environment.NewLine}{Environment.NewLine}ProductionApi Failed with the following exception :{Environment.NewLine}{InnerException?.ToString()}";
    }
}