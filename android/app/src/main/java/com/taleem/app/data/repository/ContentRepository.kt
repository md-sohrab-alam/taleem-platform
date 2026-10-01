package com.taleem.app.data.repository

import com.taleem.app.data.api.TaleemApi
import com.taleem.app.data.dto.*
import com.taleem.app.domain.UiState
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ContentRepository @Inject constructor(
    private val api: TaleemApi,
) {
    suspend fun getBooks(): UiState<List<BookSummaryDto>> = runCatching {
        val items = api.getBooks().items
        if (items.isEmpty()) UiState.Empty else UiState.Success(items)
    }.getOrElse { UiState.Error(it.message ?: "Failed to load books") }

    suspend fun getBookHome(bookId: String): UiState<BookHomeDto> = runCatching {
        UiState.Success(api.getBookHome(bookId))
    }.getOrElse { UiState.Error(it.message ?: "Failed to load book home") }

    suspend fun getChapter(chapterId: String): UiState<ChapterDetailDto> = runCatching {
        UiState.Success(api.getChapter(chapterId))
    }.getOrElse { UiState.Error(it.message ?: "Failed to load chapter") }

    suspend fun getUnit(unitId: String): UiState<TaleemUnitDetailDto> = runCatching {
        UiState.Success(api.getUnit(unitId))
    }.getOrElse { UiState.Error(it.message ?: "Failed to load unit") }

    suspend fun getTopics(bookId: String): UiState<List<TopicDto>> = runCatching {
        val items = api.getTopics(bookId)
        if (items.isEmpty()) UiState.Empty else UiState.Success(items)
    }.getOrElse { UiState.Error(it.message ?: "Failed to load topics") }

    suspend fun getTopicUnits(bookId: String, topicId: String): UiState<List<TaleemUnitSummaryDto>> = runCatching {
        val items = api.getTopicUnits(bookId, topicId)
        if (items.isEmpty()) UiState.Empty else UiState.Success(items)
    }.getOrElse { UiState.Error(it.message ?: "Failed to load topic units") }

    suspend fun getTaleem(bookId: String, minutes: Int): UiState<TaleemSelectionDto> = runCatching {
        UiState.Success(api.getTaleem(bookId, minutes))
    }.getOrElse { UiState.Error(it.message ?: "Failed to load Ta'leem") }

    suspend fun search(bookId: String, q: String): UiState<List<KeywordSearchResultDto>> = runCatching {
        val items = api.search(bookId, q)
        if (items.isEmpty()) UiState.Empty else UiState.Success(items)
    }.getOrElse { UiState.Error(it.message ?: "Search failed") }

    suspend fun semantic(bookId: String, query: String): UiState<List<SemanticSearchResultDto>> = runCatching {
        val items = api.semanticSearch(SemanticSearchRequestDto(bookId, query, 8))
        if (items.isEmpty()) UiState.Empty else UiState.Success(items)
    }.getOrElse { UiState.Error(it.message ?: "Semantic search failed") }
}
