using System;

namespace Api.Modules.Templates.Exceptions
{
    public class InvalidComponentException : Exception
    {
        public InvalidComponentException() : base() { }
        public InvalidComponentException(string message) : base(message) { }
        public InvalidComponentException(string message, Exception ex) : base(message, ex) { }
    }
}
