namespace Me.EarzuChan.Ryo.Core.Exceptions
{
    namespace CodecationExceptions
    {
        public class RyoTypeParsingException(string reason) : Exception($"Parsing Ryo Type error: {reason}");

        public class IllegalRyoTypeException(string reason) : Exception($"Illegal Ryo Type error: {reason}");
    }
}
