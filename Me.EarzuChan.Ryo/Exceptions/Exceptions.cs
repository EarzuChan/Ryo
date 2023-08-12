using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Exceptions
{
    public class RyoException : Exception
    {
        public RyoException(string message) : base(message) { }
        public RyoException(string message, Exception ex) : base(message, ex) { }
    }

    namespace FileExceptions
    {
        public class NoSuchFileException : Exception
        {
            public NoSuchFileException(string fileName) : base($"Please review your input as the file {fileName} could not be located.") { }
        }
    }
}
