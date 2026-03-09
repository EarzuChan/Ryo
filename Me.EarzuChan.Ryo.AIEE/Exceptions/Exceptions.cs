namespace Me.EarzuChan.Ryo.AIEE.Exceptions;

public sealed class EditorErrorCode
{
    private string Value { get; }
    
    private EditorErrorCode(string value) => Value = value;

    public static readonly EditorErrorCode SaveItemFailed = new("save_item_failed");
    public static readonly EditorErrorCode VolumeNotFound = new("volume_not_found");

    // 隐式转换：允许直接把对象当字符串用（例如日志输出）
    public static implicit operator string(EditorErrorCode code) => code.Value;
}