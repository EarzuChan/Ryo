package me.earzuchan.ryo.aiee.duty

enum class AppCommand {
    OpenWelcomeTab,
    OpenEditorSessionTab,
    OpenSettingsTab,
    CloseCurrentTab,
    ToggleSidePanel,
    FocusAssetsPanel,
    FocusSchemasPanel,
    ToggleMaximizeWindow,
    RequestWindowClose,
    Undo,
    Redo,
    Save,
    Discard
}

enum class AppLifecycleStage {
    Running,
    Closing
}