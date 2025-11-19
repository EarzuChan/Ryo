namespace Me.EarzuChan.Ryo.Extensions.Exceptions
{
    namespace DataTypeSchemaExceptions
    {
        public class DataTypeSchemaParsingException(string reason) : Exception($"解析DataTypeSchema时出错：{reason}");
    }
}
