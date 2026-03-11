using System;

namespace Me.EarzuChan.Ryo.Kurisu.HostBackends;

public enum KurisuWindowState {
    Normal,
    Maximized,
    Minimized
}

public sealed record KurisuHostCapabilities(
    bool SupportsWindowControls,
    bool SupportsWindowStateRead,
    bool SupportsWindowStateWrite,
    bool SupportsOpenFileDialog,
    bool SupportsSaveFileDialog
);

public interface IHostBackend {
    event Action? AppReady;
    event Action? AppClosed;

    void Close();
    void Show();
    void Init(KurisuApp app);
    void EmitWebEvent(WebLetter model);
    KurisuHostCapabilities GetHostCapabilities();
    bool TrySetWindowState(KurisuWindowState state);
    bool TryGetWindowState(out KurisuWindowState state);
    bool TryOpenFileByDialog(string fileDescription, string fileExtension, out string? filePath);
    bool TrySaveFileByDialog(string fileDescription, string fileExtension, string? suggestedFileName, out string? filePath);
}
