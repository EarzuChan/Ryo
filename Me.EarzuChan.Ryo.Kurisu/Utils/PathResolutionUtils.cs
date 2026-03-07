using System;
using System.IO;

namespace Me.EarzuChan.Ryo.Kurisu.Utils;

internal static class PathResolutionUtils {
    public static string ResolveWebResourcePath(string configuredPath) {
        if (string.IsNullOrWhiteSpace(configuredPath)) throw new ArgumentException("WebResourcePath 不能为空", nameof(configuredPath));

        if (Path.IsPathRooted(configuredPath)) return Path.GetFullPath(configuredPath);

        // 优先按程序目录解析，避免被启动时 cwd 影响（macOS/Linux 上常见）。
        var baseDirectoryCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        if (Directory.Exists(baseDirectoryCandidate)) return baseDirectoryCandidate;

        // 兼容历史行为：若程序目录不存在，则再尝试当前工作目录。
        var currentDirectoryCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configuredPath));
        return Directory.Exists(currentDirectoryCandidate) ? currentDirectoryCandidate : baseDirectoryCandidate; // 目录不存在时仍返回程序目录候选，便于统一错误提示。
    }
}
