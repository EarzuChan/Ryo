package me.earzuchan.ryo.aiee

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.window.Window
import androidx.compose.ui.window.application

fun main() = application {
    Window(onCloseRequest = ::exitApplication, title = "Ryo AIEE") {
        App()
    }
}

@Composable
private fun App() = MaterialTheme {
    Surface {
        Text("AIEE")
    }
}