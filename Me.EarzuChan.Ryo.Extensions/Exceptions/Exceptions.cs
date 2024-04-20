using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Exceptions
{
    namespace DataTypeSchemaExceptions
    {
        public class DataTypeSchemaParsingException : Exception
        {
            public DataTypeSchemaParsingException(string reason) : base($"Parsing Ts Type error: {reason}") { }
        }
    }
}
