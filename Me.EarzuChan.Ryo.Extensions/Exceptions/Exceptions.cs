using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Exceptions
{
    namespace TsTypeExceptions
    {
        public class TsTypeParsingException : Exception
        {
            public TsTypeParsingException(string reason) : base($"Parsing Ts Type error: {reason}") { }
        }
    }
}
