using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.Exceptions
{
    public class WebEventParsingException : Exception
    {
        public WebEventParsingException(string reason) : base($"Parsing Web Event error: {reason}") { }
    }
    
    public class IllegalWebEventException : Exception
    {
        public IllegalWebEventException(string reason) : base($"An illegal Web Event: {reason}") { }
    }
}
