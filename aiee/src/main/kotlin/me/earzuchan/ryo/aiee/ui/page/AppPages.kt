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
import io.github.kdroidfilter.platformtools.darkmodedetector.isSystemInDarkMode
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.duty.DialogDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.ic_apps_24px
import me.earzuchan.ryo.aiee.resources.ic_check_update_24px
import me.earzuchan.ryo.aiee.resources.ic_info_24px
import me.earzuchan.ryo.aiee.resources.ic_keyboard_24px
import me.earzuchan.ryo.aiee.resources.ic_language_24px
import me.earzuchan.ryo.aiee.resources.ic_palette_24px
import me.earzuchan.ryo.aiee.resources.ic_ui_mode_24px
import me.earzuchan.ryo.aiee.resources.illu_ryo_mark
import me.earzuchan.ryo.aiee.ui.component.ButtonType
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.util.ResUtils.vector
import org.jetbrains.compose.resources.DrawableResource
import kotlin.math.roundToInt

@Composable
fun WelcomePage() = PlaceholderPage("欢迎使用 Ryo AIEE", "这里将放置欢迎页内容")

@Composable
fun EditorSessionPage() = PlaceholderPage("编辑会话页", "这里将放置编辑会话主工作区")

@Composable
fun SettingsPage(appDuty: AppDuty) {
    @Composable
    fun SettingsSection(title: String) = Box(Modifier.fillMaxWidth().height(48.dp).padding(horizontal = 16.dp, vertical = 4.dp), Alignment.BottomStart) {
        Text(title, style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Medium)
    }

    val (languageTextAnchor, setLanguageTextAnchor) = remember { mutableStateOf(IntOffset.Zero) }
    val (themeModeTextAnchor, setThemeModeTextAnchor) = remember { mutableStateOf(IntOffset.Zero) }
    val density = LocalDensity.current

    val themeMode by appDuty.appThemeMode.collectAsState()
    val language by appDuty.appLanguage.collectAsState()

    fun openLanguageSelectMenu() {
        val options = RyoPreferences.Language.entries
        appDuty.showInPlaceSelectMenu(languageTextAnchor.x, languageTextAnchor.y, options.indexOf(language), density, options.map { option ->
            RyoMenuEntry.MenuItem(option.name, onClick = { appDuty.setAppLanguage(option) })
        })
    }

    fun openThemeModeSelectMenu() {
        val options = RyoPreferences.ThemeMode.entries
        appDuty.showInPlaceSelectMenu(themeModeTextAnchor.x, themeModeTextAnchor.y, options.indexOf(themeMode), density, options.map { option ->
            RyoMenuEntry.MenuItem(option.name, onClick = { appDuty.setAppThemeMode(option) })
        })
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 8.dp)) {
        SettingsSection("使用习惯")

        SettingsItem(Res.drawable.ic_apps_24px, "偏好编辑器设置", enabled = false) // TODO

        SettingsItem(Res.drawable.ic_keyboard_24px, "快捷键设置") {
            appDuty.dialogDuty.orderCommon(
                icon = Res.drawable.ic_keyboard_24px,
                headline = "快捷键设置",
                description = "后续会接入真实快捷键映射与冲突检测",
                actions = listOf(DialogDuty.DialogAction("知道了", ButtonType.Filled))
            )
        }

        SettingsSection("界面")
        SettingsItem(Res.drawable.ic_language_24px, "语言", trailing = language.name, onTrailingAnchorChanged = setLanguageTextAnchor, onClick = ::openLanguageSelectMenu)
        SettingsItem(Res.drawable.ic_ui_mode_24px, "模式", trailing = themeMode.name, onTrailingAnchorChanged = setThemeModeTextAnchor, onClick = ::openThemeModeSelectMenu)

        SettingsItem(Res.drawable.ic_palette_24px, "主题", trailing = "默认", enabled = false) // TODO

        SettingsSection("更多")
        SettingsItem(Res.drawable.ic_info_24px, "关于 ${BuildConfig.APP_NAME}", subtitle = "by ${BuildConfig.APP_AUTHOR}", onClick = appDuty::showAboutDialog)
        SettingsItem(Res.drawable.ic_check_update_24px, "检查更新", subtitle = BuildConfig.APP_VER, trailing = "当前已为最新", enabled = false) // TODO
        Spacer(Modifier.fillMaxWidth().height(16.dp))
    }
}

@Composable
fun EmptyPage() = Box(Modifier.fillMaxSize(), Alignment.Center) { Icon(Res.drawable.illu_ryo_mark.vector, "空页插图", Modifier.width(192.dp), MaterialTheme.colorScheme.surfaceVariant) }

@Composable
private fun PlaceholderPage(title: String, subtitle: String) = Box(Modifier.fillMaxSize().padding(horizontal = 24.dp, vertical = 16.dp), Alignment.TopStart) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(title, style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Medium)
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

@Composable
private fun SettingsItem(icon: DrawableResource, title: String, subtitle: String? = null, trailing: String? = null, enabled: Boolean = true, onTrailingAnchorChanged: ((IntOffset) -> Unit)? = null, onClick: (() -> Unit)? = null) {
    val titleColor = if (enabled) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.onSurface.copy(alpha = 0.38F)
    val sideColor = if (enabled) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.38F)
    val rowModifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).let { if (onClick == null) it else it.clickable(enabled = enabled, onClick = onClick) }.padding(horizontal = 16.dp, vertical = 12.dp)

    Row(rowModifier, verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(16.dp)) {
        Icon(icon.vector, title, Modifier.size(24.dp), sideColor)

        Column(Modifier.weight(1F), verticalArrangement = Arrangement.spacedBy(2.dp)) {
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
