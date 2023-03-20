using System;

namespace Api.Modules.Templates.Exceptions
{
    /// <inheritdoc />
    public class InvalidComponentException : Exception
    {
        /// <inheritdoc />
        public InvalidComponentException() : base() { }

        /// <inheritdoc />
        public InvalidComponentException(string message) : base(message) { }

        /// <inheritdoc />
        public InvalidComponentException(string message, Exception ex) : base(message, ex) { }
    }
}
