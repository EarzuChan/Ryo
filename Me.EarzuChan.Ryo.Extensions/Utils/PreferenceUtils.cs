using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;

namespace Me.EarzuChan.Ryo.Extensions.Utils;

public static class PreferenceUtils
{
    private static readonly string PreferencesFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "preferences.json");

    private static FileSystemWatcher _watcher;
    private static Dictionary<string, object> _preferences = new();

    public static event Action<string, object?>? PreferenceChanged;

    static PreferenceUtils()
    {
        EnsureDirectoryExists();
        LoadPreferences();
        InitializeFileSystemWatcher();
    }

    private static void InitializeFileSystemWatcher()
    {
        _watcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(PreferencesFilePath)!,
            Filter = Path.GetFileName(PreferencesFilePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnPreferencesChanged;
        _watcher.Created += OnPreferencesChanged;
        _watcher.Renamed += OnPreferencesChanged;
    }

    public static void SetPreference(string key, object value)
    {
        _preferences[key] = value;
        SavePreferences();

        PreferenceChanged?.Invoke(key, value);
    }

    public static T? GetPreference<T>(string key, T? defaultValue = default)
    {
        if (_preferences.TryGetValue(key, out var value))
        {
            if (value is T typedValue) return typedValue;

            try
            {
                var json = JsonConvert.SerializeObject(value);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                LogUtils.PrintError($"读取偏好项{key}时错误", ex);
            }
        }

        if (defaultValue != null) SetPreference(key, defaultValue);

        return defaultValue;
    }

    public static void RemovePreference(string key)
    {
        if (!_preferences.Remove(key)) return;

        SavePreferences();

        PreferenceChanged?.Invoke(key, null);
    }

    private static void LoadPreferences()
    {
        if (!File.Exists(PreferencesFilePath)) return;

        try
        {
            var json = File.ReadAllText(PreferencesFilePath);
            _preferences = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();
        }
        catch (Exception ex)
        {
            LogUtils.PrintError("加载偏好时错误", ex);
        }
    }

    private static void SavePreferences()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_preferences, Formatting.Indented);
            File.WriteAllText(PreferencesFilePath, json);
        }
        catch (Exception ex)
        {
            LogUtils.PrintError("保存偏好时错误", ex);
        }
    }

    private static void OnPreferencesChanged(object sender, FileSystemEventArgs e)
    {
        LoadPreferences();
        
        // TODO：有空做个比较，只触发修改的事件
        
        foreach (var key in _preferences.Keys)
        {
            PreferenceChanged?.Invoke(key, _preferences[key]);
        }
    }

    private static void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(PreferencesFilePath);
        if (directory != null) Directory.CreateDirectory(directory);
    }

    public static void ClearPreferences()
    {
        foreach (var key in _preferences.Keys)
        {
            PreferenceChanged?.Invoke(key, null);
        }

        _preferences.Clear();
        SavePreferences();
    }
}
