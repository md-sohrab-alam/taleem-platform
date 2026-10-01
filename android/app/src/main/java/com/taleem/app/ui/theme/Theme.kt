package com.taleem.app.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val Teal = Color(0xFF0F6B4C)
private val Soft = Color(0xFFE7F3EC)
private val Ink = Color(0xFF13261D)

private val colors = lightColorScheme(
    primary = Teal,
    onPrimary = Color.White,
    secondary = Color(0xFF2F8F6D),
    background = Color(0xFFF4F7F5),
    surface = Color.White,
    onBackground = Ink,
    onSurface = Ink,
    primaryContainer = Soft,
)

@Composable
fun TaleemTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = colors, content = content)
}
