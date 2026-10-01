package com.taleem.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.taleem.app.data.dto.*
import com.taleem.app.data.local.PrefsStore
import com.taleem.app.data.repository.ContentRepository
import com.taleem.app.domain.UiState
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class LibraryViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<List<BookSummaryDto>>>(UiState.Loading)
    val state: StateFlow<UiState<List<BookSummaryDto>>> = _state
    fun load() = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.getBooks()
    }
}

@HiltViewModel
class BookHomeViewModel @Inject constructor(
    private val repo: ContentRepository,
    private val prefs: PrefsStore,
) : ViewModel() {
    private val _state = MutableStateFlow<UiState<BookHomeDto>>(UiState.Loading)
    val state: StateFlow<UiState<BookHomeDto>> = _state
    fun load(bookId: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        prefs.setLastBook(bookId)
        _state.value = repo.getBookHome(bookId)
    }
}

@HiltViewModel
class ChapterViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<ChapterDetailDto>>(UiState.Loading)
    val state: StateFlow<UiState<ChapterDetailDto>> = _state
    fun load(chapterId: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.getChapter(chapterId)
    }
}

@HiltViewModel
class ReaderViewModel @Inject constructor(
    private val repo: ContentRepository,
    private val prefs: PrefsStore,
) : ViewModel() {
    private val _state = MutableStateFlow<UiState<TaleemUnitDetailDto>>(UiState.Loading)
    val state: StateFlow<UiState<TaleemUnitDetailDto>> = _state
    fun load(unitId: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        prefs.setLastUnit(unitId)
        _state.value = repo.getUnit(unitId)
    }
}

@HiltViewModel
class TopicsViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<List<TopicDto>>>(UiState.Loading)
    val state: StateFlow<UiState<List<TopicDto>>> = _state
    private val _units = MutableStateFlow<UiState<List<TaleemUnitSummaryDto>>>(UiState.Empty)
    val units: StateFlow<UiState<List<TaleemUnitSummaryDto>>> = _units
    fun load(bookId: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.getTopics(bookId)
    }
    fun loadUnits(bookId: String, topicId: String) = viewModelScope.launch {
        _units.value = UiState.Loading
        _units.value = repo.getTopicUnits(bookId, topicId)
    }
}

@HiltViewModel
class SearchViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<List<KeywordSearchResultDto>>>(UiState.Empty)
    val state: StateFlow<UiState<List<KeywordSearchResultDto>>> = _state
    fun search(bookId: String, q: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.search(bookId, q)
    }
}

@HiltViewModel
class SemanticSearchViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<List<SemanticSearchResultDto>>>(UiState.Empty)
    val state: StateFlow<UiState<List<SemanticSearchResultDto>>> = _state
    fun search(bookId: String, q: String) = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.semantic(bookId, q)
    }
}

@HiltViewModel
class TaleemViewModel @Inject constructor(private val repo: ContentRepository) : ViewModel() {
    private val _state = MutableStateFlow<UiState<TaleemSelectionDto>>(UiState.Loading)
    val state: StateFlow<UiState<TaleemSelectionDto>> = _state
    fun load(bookId: String, minutes: Int) = viewModelScope.launch {
        _state.value = UiState.Loading
        _state.value = repo.getTaleem(bookId, minutes)
    }
}
