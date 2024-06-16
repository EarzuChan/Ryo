using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.Exceptions
{
    public class WebEventParsingException(string reason) : Exception($"Parsing Web Event error: {reason}");

    public class WebCallParsingException(string reason) : Exception($"Parsing Web Call error: {reason}");

    public class IllegalWebEventException(string reason) : Exception($"An illegal Web Event: {reason}");

    public class IllegalWebCallException(string reason) : Exception($"An illegal Web Call: {reason}");

    public class WinWebAppBuildingException(string reason) : Exception($"Building your Win Web App error: {reason}");
}
