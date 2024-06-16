using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Core.Exceptions
{
    namespace AdaptationExceptions
    {
        public class RyoTypeParsingException : Exception
        {
            public RyoTypeParsingException(string reason) : base($"Parsing Ryo Type error: {reason}") { }
        }

        public class IllegalRyoTypeException : Exception
        {
            public IllegalRyoTypeException(string reason) : base($"Illegal Ryo Type error: {reason}") { }
        }
    }
}
