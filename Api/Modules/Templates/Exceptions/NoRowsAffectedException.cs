using System;

namespace Api.Modules.Templates.Exceptions
{
    /// <inheritdoc />
    public class NoRowsAffectedException : Exception
    {
        /// <inheritdoc />
        public NoRowsAffectedException() : base() { }

        /// <inheritdoc />
        public NoRowsAffectedException(string message) : base(message) { }

        /// <inheritdoc />
        public NoRowsAffectedException(string message, Exception ex) : base(message, ex) { }
    }
}
