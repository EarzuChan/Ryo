using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Extensions.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class PreferenceUtils
{
    private static readonly string PreferencesFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "preferences.json");

    private static Dictionary<string, object> preferences;

    static PreferenceUtils() => LoadPreferences();

    private static void LoadPreferences()
    {
        if (File.Exists(PreferencesFilePath))
        {
            var json = File.ReadAllText(PreferencesFilePath);
            preferences = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
        else
        {
            preferences = new Dictionary<string, object>();
        }
    }

    private static void SavePreferences()
    {
        var directory = Path.GetDirectoryName(PreferencesFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonConvert.SerializeObject(preferences, Formatting.Indented);
        File.WriteAllText(PreferencesFilePath, json);
    }

    public static void SetPreference(string key, object value)
    {
        preferences[key] = value;
        SavePreferences();
    }

    public static T GetPreference<T>(string key, T defaultValue = default(T))
    {
        LoadPreferences();
        if (preferences.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        defaultValue.Ensure(obj => SetPreference(key, obj));
        return defaultValue;
    }

    public static bool HasPreference(string key)
    {
        LoadPreferences();
        return preferences.ContainsKey(key);
    }

    public static void RemovePreference(string key)
    {
        LoadPreferences();
        if (preferences.Remove(key))
        {
            SavePreferences();
        }
    }

    public static void ClearPreferences()
    {
        preferences.Clear();
        SavePreferences();
    }
}