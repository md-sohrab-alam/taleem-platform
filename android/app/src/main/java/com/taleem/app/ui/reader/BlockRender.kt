package com.taleem.app.ui.reader

import androidx.compose.ui.unit.LayoutDirection
import com.taleem.app.data.dto.ContentBlockDto

object BlockRender {
    fun isRtl(block: ContentBlockDto): Boolean {
        val lang = block.language.lowercase()
        if (lang == "ar" || lang == "ur") return true
        return block.blockType.contains("ARABIC", ignoreCase = true)
    }

    fun layoutDirection(block: ContentBlockDto): LayoutDirection =
        if (isRtl(block)) LayoutDirection.Rtl else LayoutDirection.Ltr

    fun isHeading(block: ContentBlockDto): Boolean = block.blockType.equals("HEADING", true)

    fun isSacredQuote(block: ContentBlockDto): Boolean =
        block.blockType.contains("QURAN", true) || block.blockType.contains("HADITH", true)
}
