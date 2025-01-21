using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Exceptions
{
    namespace DataTypeSchemaExceptions
    {
        public class DataTypeSchemaParsingException(string reason) : Exception($"解析Ts类型时错误：{reason}");
    }
}
