using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Masses;

namespace Me.EarzuChan.Ryo.Core.Utils
{
    public static class ReadWriteTypesUtils
    {
        public static object Read(Type itemType, RyoReader reader, Mass mass, bool isFieldMode = false) =>
           (itemType == typeof(int)) ? reader.ReadInt() :
           (itemType == typeof(string) && !isFieldMode) ? reader.ReadString() :
           (itemType == typeof(float)) ? reader.ReadFloat() :
           (itemType == typeof(bool)) ? reader.ReadBoolean() :
           mass.Read<object>();

        public static void Write(Type itemType, object item, RyoWriter writer, Mass mass, bool isFieldMode = false)
        {
            if (itemType == typeof(int)) writer.WriteInt((int)item);
            else if (itemType == typeof(string) && !isFieldMode) writer.WrintString((string)item);
            else if (itemType == typeof(float)) writer.WriteFloat((float)item);
            else if (itemType == typeof(bool)) writer.WriteBoolean((bool)item);
            else mass.Write<object>(item);
        }

    }
}
