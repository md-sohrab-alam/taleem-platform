package com.taleem.app

import com.google.common.truth.Truth.assertThat
import com.taleem.app.data.dto.ContentBlockDto
import com.taleem.app.ui.reader.BlockRender
import org.junit.Test

class BlockRenderTest {
    @Test
    fun arabicBlocksUseRtl() {
        val block = ContentBlockDto("1", "QURAN_ARABIC", "ar", "بسم الله")
        assertThat(BlockRender.isRtl(block)).isTrue()
    }

    @Test
    fun englishParagraphsUseLtr() {
        val block = ContentBlockDto("1", "PARAGRAPH", "en", "Prayer is...")
        assertThat(BlockRender.isRtl(block)).isFalse()
    }
}
