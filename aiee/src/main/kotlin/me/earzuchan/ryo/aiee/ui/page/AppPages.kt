package me.earzuchan.ryo.aiee.ui.page

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.clickable
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.boundsInWindow
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.duty.DialogDuty
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.ButtonType
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import me.earzuchan.ryo.aiee.util.UiUtils.text
import org.jetbrains.compose.resources.DrawableResource
import kotlin.math.roundToInt

@Composable
fun WelcomePage() = PlaceholderPage(Res.string.page_welcome_title.text, Res.string.page_welcome_subtitle.text)

@Composable
fun EditorSessionPage() = PlaceholderPage(Res.string.page_editor_session_title.text, Res.string.page_editor_session_subtitle.text)

@Composable
fun SettingsPage(appDuty: AppDuty) {
    @Composable
    fun Section(title: String) = Box(Modifier.fillMaxWidth().height(48.dp).padding(horizontal = 16.dp, vertical = 4.dp), Alignment.BottomStart) {
        Text(title, style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Medium)
    }

    @Composable
    fun Item(icon: DrawableResource, title: String, subtitle: String? = null, trailing: String? = null, enabled: Boolean = true, onTrailingAnchorChanged: ((IntOffset) -> Unit)? = null, onClick: (() -> Unit)? = null) {
        val titleColor = if (enabled) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.onSurface.copy(alpha = 0.38F)
        val sideColor = if (enabled) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.38F)
        val rowModifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).let { if (onClick == null) it else it.clickable(enabled = enabled, onClick = onClick) }.padding(horizontal = 16.dp, vertical = 12.dp)

        Row(rowModifier, verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(16.dp)) {
            Icon(icon.vector, title, Modifier.size(24.dp), sideColor)

            Column(Modifier.weight(1F)) {
                Text(title, style = MaterialTheme.typography.bodyLarge, color = titleColor)
                if (subtitle != null) Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = sideColor)
            }

            if (trailing != null) Text(
                trailing,
                Modifier.onGloballyPositioned { coords ->
                    val b = coords.boundsInWindow()
                    onTrailingAnchorChanged?.invoke(IntOffset(b.left.roundToInt(), ((b.top + b.bottom) / 2F).roundToInt()))
                },
                color = sideColor,
                style = MaterialTheme.typography.bodyLarge
            )
        }
    }


    val (languageTextAnchor, setLanguageTextAnchor) = remember { mutableStateOf(IntOffset.Zero) }
    val (themeModeTextAnchor, setThemeModeTextAnchor) = remember { mutableStateOf(IntOffset.Zero) }
    val density = LocalDensity.current

    val themeMode by appDuty.appThemeMode.collectAsState()
    val language by appDuty.appLanguage.collectAsState()
    val shortcutsDialogTitle = Res.string.set_shortcuts.text
    val shortcutsDialogDescription = Res.string.settings_shortcuts_dialog_description.text
    val gotIt = Res.string.action_got_it.text

    val languageOptionLabels = RyoPreferences.Language.entries.associateWith { option ->
        when (option) {
            RyoPreferences.Language.SYSTEM -> Res.string.language_option_system.text
            RyoPreferences.Language.CHINESE -> Res.string.language_option_chinese.text
            RyoPreferences.Language.ENGLISH -> Res.string.language_option_english.text
        }
    }
    val themeModeOptionLabels = RyoPreferences.ThemeMode.entries.associateWith { option ->
        when (option) {
            RyoPreferences.ThemeMode.SYSTEM -> Res.string.theme_mode_system.text
            RyoPreferences.ThemeMode.DARK -> Res.string.theme_mode_dark.text
            RyoPreferences.ThemeMode.LIGHT -> Res.string.theme_mode_light.text
        }
    }

    fun openLanguageSelectMenu() {
        val options = RyoPreferences.Language.entries
        appDuty.showInPlaceSelectMenu(languageTextAnchor.x, languageTextAnchor.y, options.indexOf(language), density, options.map { option ->
            RyoMenuEntry.MenuItem(languageOptionLabels.getValue(option), onClick = { appDuty.setAppLanguage(option) })
        })
    }

    fun openThemeModeSelectMenu() {
        val options = RyoPreferences.ThemeMode.entries
        appDuty.showInPlaceSelectMenu(themeModeTextAnchor.x, themeModeTextAnchor.y, options.indexOf(themeMode), density, options.map { option ->
            RyoMenuEntry.MenuItem(themeModeOptionLabels.getValue(option), onClick = { appDuty.setAppThemeMode(option) })
        })
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 8.dp)) {
        Section(Res.string.settings_section_usage.text)

        Item(Res.drawable.ic_apps_24px, Res.string.preferred_editors.text, enabled = false) // TODO

        Item(Res.drawable.ic_keyboard_24px, Res.string.set_shortcuts.text) {
            // TODO
            appDuty.dialogDuty.orderCommon(
                icon = Res.drawable.ic_keyboard_24px,
                headline = UiText.Plain(shortcutsDialogTitle),
                description = UiText.Plain(shortcutsDialogDescription),
                actions = listOf(DialogDuty.DialogAction(UiText.Plain(gotIt)))
            )
        }

        Section(Res.string.settings_section_interface.text)
        Item(Res.drawable.ic_language_24px, Res.string.settings_item_language.text, trailing = languageOptionLabels.getValue(language), onTrailingAnchorChanged = setLanguageTextAnchor, onClick = ::openLanguageSelectMenu)
        Item(Res.drawable.ic_ui_mode_24px, Res.string.settings_item_mode.text, trailing = themeModeOptionLabels.getValue(themeMode), onTrailingAnchorChanged = setThemeModeTextAnchor, onClick = ::openThemeModeSelectMenu)

        Item(Res.drawable.ic_palette_24px, Res.string.settings_item_theme.text, trailing = Res.string.settings_theme_default.text, enabled = false) // TODO

        Section(Res.string.settings_section_more.text)
        Item(Res.drawable.ic_info_24px, Res.string.settings_item_about_format.text(BuildConfig.APP_NAME), subtitle = Res.string.settings_item_about_subtitle_format.text(BuildConfig.APP_AUTHOR), onClick = appDuty::showAboutDialog)
        Item(Res.drawable.ic_check_update_24px, Res.string.settings_item_check_update.text, subtitle = BuildConfig.APP_VER, trailing = Res.string.settings_update_latest.text, enabled = false) // TODO
        Spacer(Modifier.fillMaxWidth().height(16.dp))
    }
}

@Composable
fun EmptyPage() = Box(Modifier.fillMaxSize(), Alignment.Center) { Icon(Res.drawable.illu_ryo_mark.vector, Res.string.empty_page_illustration_cd.text, Modifier.width(192.dp), MaterialTheme.colorScheme.outline) }

@Composable
private fun PlaceholderPage(title: String, subtitle: String) = Box(Modifier.fillMaxSize().padding(horizontal = 24.dp, vertical = 16.dp), Alignment.TopStart) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(title, style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Medium)
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}
