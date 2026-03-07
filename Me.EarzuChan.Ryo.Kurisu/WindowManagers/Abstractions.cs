using System;

namespace Me.EarzuChan.Ryo.Kurisu.WindowManagers;

public enum KurisuWindowState {
    Normal,
    Maximized,
    Minimized
}

public sealed record KurisuHostCapabilities(bool SupportsWindowControls);

public interface IKurisuWindowManager {
    event Action? AppReady;
    event Action? AppClosed;

    void SetWindowState(KurisuWindowState state);
    void Close();
    void Show();
    void Init(KurisuApp app);
    void EmitWebEvent(WebLetter model);
    KurisuWindowState GetWindowState();
    KurisuHostCapabilities GetHostCapabilities();
}
