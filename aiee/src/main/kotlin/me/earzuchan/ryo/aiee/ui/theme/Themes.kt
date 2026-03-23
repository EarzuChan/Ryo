package me.earzuchan.ryo.aiee.ui.theme

import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.graphics.Color
import io.github.kdroidfilter.platformtools.darkmodedetector.isSystemInDarkMode
import kotlinx.coroutines.flow.stateIn
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository

val primaryLight = Color(0xFF275EA7)
val onPrimaryLight = Color(0xFFFFFFFF)
val primaryContainerLight = Color(0xFFD6E3FF)
val onPrimaryContainerLight = Color(0xFF001B3D)
val secondaryLight = Color(0xFF984061)
val onSecondaryLight = Color(0xFFFFFFFF)
val secondaryContainerLight = Color(0xFFFFD9E2)
val onSecondaryContainerLight = Color(0xFF3E001D)
val tertiaryLight = Color(0xFF686000)
val onTertiaryLight = Color(0xFFFFFFFF)
val tertiaryContainerLight = Color(0xFFF8E520)
val onTertiaryContainerLight = Color(0xFF1F1C00)
val errorLight = Color(0xFFC0001E)
val onErrorLight = Color(0xFFFFFFFF)
val errorContainerLight = Color(0xFFFFDAD7)
val onErrorContainerLight = Color(0xFF410004)
val backgroundLight = Color(0xFFFDFBFF)
val onBackgroundLight = Color(0xFF1A1B1E)
val surfaceLight = Color(0xFFFAF9FD)
val onSurfaceLight = Color(0xFF1A1B1E)
val surfaceVariantLight = Color(0xFFE0E2EC)
val onSurfaceVariantLight = Color(0xFF43474E)
val outlineLight = Color(0xFF74777F)
val outlineVariantLight = Color(0xFFC4C6CF)
val scrimLight = Color(0xFF000000)
val inverseSurfaceLight = Color(0xFF2F3033)
val inverseOnSurfaceLight = Color(0xFFF1F0F4)
val inversePrimaryLight = Color(0xFFA9C7FF)
val surfaceDimLight = Color(0xFFDAD9DD)
val surfaceBrightLight = Color(0xFFFAF9FD)
val surfaceContainerLowestLight = Color(0xFFFFFFFF)
val surfaceContainerLowLight = Color(0xFFF4F3F7)
val surfaceContainerLight = Color(0xFFEFEDF1)
val surfaceContainerHighLight = Color(0xFFE9E7EB)
val surfaceContainerHighestLight = Color(0xFFE3E2E6)

val primaryDark = Color(0xFFA9C7FF)
val onPrimaryDark = Color(0xFF003063)
val primaryContainerDark = Color(0xFF00468B)
val onPrimaryContainerDark = Color(0xFFD6E3FF)
val secondaryDark = Color(0xFFFFB1C8)
val onSecondaryDark = Color(0xFF5E1133)
val secondaryContainerDark = Color(0xFF7B2949)
val onSecondaryContainerDark = Color(0xFFFFD9E2)
val tertiaryDark = Color(0xFFDAC900)
val onTertiaryDark = Color(0xFF363100)
val tertiaryContainerDark = Color(0xFF4F4800)
val onTertiaryContainerDark = Color(0xFFF8E520)
val errorDark = Color(0xFFFFB3AE)
val onErrorDark = Color(0xFF68000B)
val errorContainerDark = Color(0xFF930015)
val onErrorContainerDark = Color(0xFFFFDAD7)
val backgroundDark = Color(0xFF1A1B1E)
val onBackgroundDark = Color(0xFFE3E2E6)
val surfaceDark = Color(0xFF121316)
val onSurfaceDark = Color(0xFFC7C6CA)
val surfaceVariantDark = Color(0xFF43474E)
val onSurfaceVariantDark = Color(0xFFC4C6CF)
val outlineDark = Color(0xFF8E9099)
val outlineVariantDark = Color(0xFF43474E)
val scrimDark = Color(0xFF000000)
val inverseSurfaceDark = Color(0xFFE3E2E6)
val inverseOnSurfaceDark = Color(0xFF1A1B1E)
val inversePrimaryDark = Color(0xFF275EA7)
val surfaceDimDark = Color(0xFF121316)
val surfaceBrightDark = Color(0xFF38393C)
val surfaceContainerLowestDark = Color(0xFF0D0E11)
val surfaceContainerLowDark = Color(0xFF1A1B1E)
val surfaceContainerDark = Color(0xFF1E2023)
val surfaceContainerHighDark = Color(0xFF292A2D)
val surfaceContainerHighestDark = Color(0xFF343538)

val ryoLightColorScheme = lightColorScheme(
    primary = primaryLight,
    onPrimary = onPrimaryLight,
    primaryContainer = primaryContainerLight,
    onPrimaryContainer = onPrimaryContainerLight,
    secondary = secondaryLight,
    onSecondary = onSecondaryLight,
    secondaryContainer = secondaryContainerLight,
    onSecondaryContainer = onSecondaryContainerLight,
    tertiary = tertiaryLight,
    onTertiary = onTertiaryLight,
    tertiaryContainer = tertiaryContainerLight,
    onTertiaryContainer = onTertiaryContainerLight,
    error = errorLight,
    onError = onErrorLight,
    errorContainer = errorContainerLight,
    onErrorContainer = onErrorContainerLight,
    background = backgroundLight,
    onBackground = onBackgroundLight,
    surface = surfaceLight,
    onSurface = onSurfaceLight,
    surfaceVariant = surfaceVariantLight,
    onSurfaceVariant = onSurfaceVariantLight,
    outline = outlineLight,
    outlineVariant = outlineVariantLight,
    scrim = scrimLight,
    inverseSurface = inverseSurfaceLight,
    inverseOnSurface = inverseOnSurfaceLight,
    inversePrimary = inversePrimaryLight,
    surfaceDim = surfaceDimLight,
    surfaceBright = surfaceBrightLight,
    surfaceContainerLowest = surfaceContainerLowestLight,
    surfaceContainerLow = surfaceContainerLowLight,
    surfaceContainer = surfaceContainerLight,
    surfaceContainerHigh = surfaceContainerHighLight,
    surfaceContainerHighest = surfaceContainerHighestLight
)

val ryoDarkColorScheme = darkColorScheme(
    primary = primaryDark,
    onPrimary = onPrimaryDark,
    primaryContainer = primaryContainerDark,
    onPrimaryContainer = onPrimaryContainerDark,
    secondary = secondaryDark,
    onSecondary = onSecondaryDark,
    secondaryContainer = secondaryContainerDark,
    onSecondaryContainer = onSecondaryContainerDark,
    tertiary = tertiaryDark,
    onTertiary = onTertiaryDark,
    tertiaryContainer = tertiaryContainerDark,
    onTertiaryContainer = onTertiaryContainerDark,
    error = errorDark,
    onError = onErrorDark,
    errorContainer = errorContainerDark,
    onErrorContainer = onErrorContainerDark,
    background = backgroundDark,
    onBackground = onBackgroundDark,
    surface = surfaceDark,
    onSurface = onSurfaceDark,
    surfaceVariant = surfaceVariantDark,
    onSurfaceVariant = onSurfaceVariantDark,
    outline = outlineDark,
    outlineVariant = outlineVariantDark,
    scrim = scrimDark,
    inverseSurface = inverseSurfaceDark,
    inverseOnSurface = inverseOnSurfaceDark,
    inversePrimary = inversePrimaryDark,
    surfaceDim = surfaceDimDark,
    surfaceBright = surfaceBrightDark,
    surfaceContainerLowest = surfaceContainerLowestDark,
    surfaceContainerLow = surfaceContainerLowDark,
    surfaceContainer = surfaceContainerDark,
    surfaceContainerHigh = surfaceContainerHighDark,
    surfaceContainerHighest = surfaceContainerHighestDark
)

@Composable
fun RyoTheme(
    useDarkTheme: Boolean= isSystemInDarkMode(),
    darkColorScheme: ColorScheme = ryoDarkColorScheme,
    lightColorScheme: ColorScheme = ryoLightColorScheme,
    content: @Composable () -> Unit
) {
    val colors = if (useDarkTheme) darkColorScheme else lightColorScheme
    MaterialTheme(colorScheme = colors, content = content)
}
