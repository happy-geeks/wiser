﻿using System;

namespace Api.Modules.Templates.Exceptions
{
    /// <inheritdoc />
    public class NoRowsAffectedException : Exception
    {
        public NoRowsAffectedException() : base() { }
        public NoRowsAffectedException(string message) : base(message) { }
        public NoRowsAffectedException(string message, Exception ex) : base(message, ex) { }
    }
}
