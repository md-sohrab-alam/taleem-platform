package com.taleem.app.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class PagedBooksDto(val items: List<BookSummaryDto> = emptyList(), val page: Int = 1, val pageSize: Int = 20, val totalCount: Int = 0)

@Serializable
data class BookSummaryDto(
    val id: String,
    val slug: String,
    val title: String,
    val subtitle: String? = null,
    val author: String? = null,
    val description: String? = null,
    val coverUrl: String? = null,
    val defaultLanguage: String = "en",
    val status: String = "PUBLISHED",
)

@Serializable
data class BookHomeDto(
    val book: BookSummaryDto,
    val continueReading: ContinueReadingDto? = null,
    val featuredReadings: List<TaleemUnitSummaryDto> = emptyList(),
    val topics: List<TopicDto> = emptyList(),
    val chapters: List<ChapterSummaryDto> = emptyList(),
    val suggestedTaleem: List<SuggestedTaleemDto> = emptyList(),
    val popularPlaceholder: List<TaleemUnitSummaryDto> = emptyList(),
    val readingOptions: List<ReadingOptionDto> = emptyList(),
)

@Serializable
data class ContinueReadingDto(val unitId: String? = null, val unitTitle: String? = null, val chapterId: String? = null)

@Serializable
data class TopicDto(val id: String, val slug: String, val displayName: String)

@Serializable
data class ChapterSummaryDto(
    val id: String,
    val title: String,
    val description: String? = null,
    val slug: String? = null,
    val orderIndex: Int = 0,
    val sourcePageStart: Int? = null,
    val sourcePageEnd: Int? = null,
    val sectionId: String? = null,
    val unitCount: Int = 0,
)

@Serializable
data class TaleemUnitSummaryDto(
    val id: String,
    val chapterId: String,
    val title: String,
    val estimatedReadingSeconds: Int = 0,
    val sourcePageStart: Int? = null,
    val sourcePageEnd: Int? = null,
    val orderIndex: Int = 0,
)

@Serializable
data class SuggestedTaleemDto(val minutes: Int, val units: List<TaleemUnitSummaryDto> = emptyList(), val approximateSeconds: Int = 0)

@Serializable
data class ReadingOptionDto(val key: String, val label: String, val hrefHint: String? = null)

@Serializable
data class ContentBlockDto(
    val id: String,
    val blockType: String,
    val language: String,
    val text: String,
    val orderIndex: Int = 0,
    val sourcePage: Int? = null,
    val sourceReference: String? = null,
    val verificationStatus: String = "UNVERIFIED",
)

@Serializable
data class TaleemUnitDetailDto(
    val id: String,
    val bookId: String,
    val chapterId: String,
    val title: String,
    val estimatedReadingSeconds: Int = 0,
    val sourcePageStart: Int? = null,
    val sourcePageEnd: Int? = null,
    val orderIndex: Int = 0,
    val reviewStatus: String = "PENDING",
    val blocks: List<ContentBlockDto> = emptyList(),
    val topics: List<TopicDto> = emptyList(),
    val chapterTitle: String? = null,
)

@Serializable
data class ChapterDetailDto(
    val id: String,
    val bookId: String,
    val title: String,
    val description: String? = null,
    val slug: String? = null,
    val orderIndex: Int = 0,
    val sourcePageStart: Int? = null,
    val sourcePageEnd: Int? = null,
    val sectionId: String? = null,
    val units: List<TaleemUnitSummaryDto> = emptyList(),
)

@Serializable
data class KeywordSearchResultDto(
    val bookId: String,
    val bookTitle: String,
    val chapterId: String,
    val chapterTitle: String,
    val unitId: String,
    val unitTitle: String,
    val excerpt: String,
    val sourcePage: Int? = null,
    val rank: Double? = null,
)

@Serializable
data class SemanticSearchRequestDto(val bookId: String? = null, val query: String, val limit: Int = 5)

@Serializable
data class SemanticSearchResultDto(
    val similarity: Double,
    val bookId: String,
    val bookTitle: String,
    val chapterId: String,
    val chapterTitle: String,
    val unitId: String,
    val unitTitle: String,
    val canonicalBlocks: List<ContentBlockDto> = emptyList(),
    val chunkExcerpt: String,
    val sourcePageStart: Int? = null,
    val sourcePageEnd: Int? = null,
)

@Serializable
data class TaleemSelectionDto(
    val requestedMinutes: Int,
    val approximateSeconds: Int,
    val units: List<TaleemUnitSummaryDto> = emptyList(),
)
