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
            public NoSuchFileException(string fileName) : base($"No such a file: {fileName}") { }
        }

        public class EmptyFileException : Exception
        {
            public EmptyFileException(string fileName) : base("An empty file was encountered: {fileName}") { }
        }

        public class RestrictedFileAccessException : Exception
        {
            public enum RestrictedFileAccess
            {
                Read,
                Write
            }

            public RestrictedFileAccessException(string fileName, RestrictedFileAccess permission) : base($"No {permission} permission for file: {fileName}") { }
        }
    }
}
