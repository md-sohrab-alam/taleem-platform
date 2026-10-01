package com.taleem.app.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.taleem.app.data.dto.ContentBlockDto
import com.taleem.app.domain.UiState
import com.taleem.app.ui.components.StateScaffold
import com.taleem.app.ui.reader.BlockRender
import com.taleem.app.ui.viewmodel.*
import kotlinx.coroutines.delay

@Composable
fun SplashScreen(onDone: () -> Unit) {
    LaunchedEffect(Unit) {
        delay(700)
        onDone()
    }
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Text("Taleem", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.SemiBold)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LibraryScreen(onOpenBook: (String) -> Unit, onSettings: () -> Unit, vm: LibraryViewModel = hiltViewModel()) {
    LaunchedEffect(Unit) { vm.load() }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Library") }, actions = {
            TextButton(onClick = onSettings) { Text("Settings") }
        })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = vm::load) { books ->
                LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    items(books) { book ->
                        ElevatedCard(Modifier.fillMaxWidth().clickable { onOpenBook(book.id) }) {
                            Column(Modifier.padding(16.dp)) {
                                Text(book.title, style = MaterialTheme.typography.titleLarge)
                                book.author?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                            }
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BookHomeScreen(
    bookId: String,
    onOpenChapter: (String) -> Unit,
    onOpenUnit: (String) -> Unit,
    onTopics: () -> Unit,
    onSearch: () -> Unit,
    onSemantic: () -> Unit,
    onTaleem: (Int) -> Unit,
    onBack: () -> Unit,
    vm: BookHomeViewModel = hiltViewModel(),
) {
    LaunchedEffect(bookId) { vm.load(bookId) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Book") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.load(bookId) }) { home ->
                LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    item {
                        Text(home.book.title, style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.SemiBold)
                        home.book.description?.let { Text(it, Modifier.padding(top = 8.dp)) }
                    }
                    item {
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            AssistChip(onClick = onSearch, label = { Text("Search") })
                            AssistChip(onClick = onSemantic, label = { Text("Semantic") })
                            AssistChip(onClick = onTopics, label = { Text("Topics") })
                        }
                    }
                    item {
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            listOf(5, 10, 15).forEach { m ->
                                AssistChip(onClick = { onTaleem(m) }, label = { Text("$m min") })
                            }
                        }
                    }
                    item { Text("Chapters", style = MaterialTheme.typography.titleMedium) }
                    items(home.chapters) { c ->
                        ElevatedCard(Modifier.fillMaxWidth().clickable { onOpenChapter(c.id) }) {
                            Column(Modifier.padding(14.dp)) {
                                Text(c.title, fontWeight = FontWeight.Medium)
                                Text("${c.unitCount} readings", color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                    }
                    item { Text("Featured", style = MaterialTheme.typography.titleMedium) }
                    items(home.featuredReadings) { u ->
                        TextButton(onClick = { onOpenUnit(u.id) }) { Text(u.title) }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ChapterScreen(chapterId: String, onOpenUnit: (String) -> Unit, onBack: () -> Unit, vm: ChapterViewModel = hiltViewModel()) {
    LaunchedEffect(chapterId) { vm.load(chapterId) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Chapter") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.load(chapterId) }) { chapter ->
                LazyColumn(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    item { Text(chapter.title, style = MaterialTheme.typography.headlineSmall) }
                    items(chapter.units) { u ->
                        ElevatedCard(Modifier.fillMaxWidth().clickable { onOpenUnit(u.id) }) {
                            Text(u.title, Modifier.padding(14.dp))
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReaderScreen(unitId: String, onBack: () -> Unit, vm: ReaderViewModel = hiltViewModel()) {
    LaunchedEffect(unitId) { vm.load(unitId) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Reader") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.load(unitId) }) { unit ->
                LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(16.dp)) {
                    item {
                        Text(unit.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                        unit.chapterTitle?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                    }
                    items(unit.blocks) { block -> ContentBlockView(block) }
                }
            }
        }
    }
}

@Composable
fun ContentBlockView(block: ContentBlockDto) {
    CompositionLocalProvider(LocalLayoutDirection provides BlockRender.layoutDirection(block)) {
        Column {
            Text(
                text = block.text,
                fontSize = if (BlockRender.isHeading(block)) 22.sp else if (BlockRender.isSacredQuote(block)) 20.sp else 17.sp,
                fontWeight = if (BlockRender.isHeading(block)) FontWeight.SemiBold else FontWeight.Normal,
                lineHeight = 28.sp,
            )
            block.sourcePage?.let {
                Text("p.$it · ${block.blockType}", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TopicsScreen(bookId: String, onOpenUnit: (String) -> Unit, onBack: () -> Unit, vm: TopicsViewModel = hiltViewModel()) {
    LaunchedEffect(bookId) { vm.load(bookId) }
    var selectedTopic by remember { mutableStateOf<String?>(null) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Topics") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.load(bookId) }) { topics ->
                Column(Modifier.padding(16.dp)) {
                    topics.forEach { t ->
                        AssistChip(
                            onClick = {
                                selectedTopic = t.id
                                vm.loadUnits(bookId, t.id)
                            },
                            label = { Text(t.displayName) },
                            modifier = Modifier.padding(end = 8.dp, bottom = 8.dp)
                        )
                    }
                    when (val units = vm.units.collectAsState().value) {
                        is UiState.Success -> units.data.forEach { u ->
                            TextButton(onClick = { onOpenUnit(u.id) }) { Text(u.title) }
                        }
                        else -> {}
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SearchScreen(bookId: String, onOpenUnit: (String) -> Unit, onBack: () -> Unit, vm: SearchViewModel = hiltViewModel()) {
    var q by remember { mutableStateOf("") }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Search") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Column(Modifier.padding(padding).padding(16.dp)) {
            OutlinedTextField(value = q, onValueChange = { q = it }, modifier = Modifier.fillMaxWidth(), label = { Text("Query") })
            Button(onClick = { vm.search(bookId, q) }, modifier = Modifier.padding(vertical = 12.dp)) { Text("Search") }
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.search(bookId, q) }) { results ->
                results.forEach { r ->
                    ElevatedCard(Modifier.fillMaxWidth().padding(bottom = 8.dp).clickable { onOpenUnit(r.unitId) }) {
                        Column(Modifier.padding(12.dp)) {
                            Text(r.unitTitle, fontWeight = FontWeight.Medium)
                            Text(r.excerpt, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SemanticSearchScreen(bookId: String, onOpenUnit: (String) -> Unit, onBack: () -> Unit, vm: SemanticSearchViewModel = hiltViewModel()) {
    var q by remember { mutableStateOf("") }
    Scaffold(topBar = {
        TopAppBar(title = { Text("Semantic search") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Column(Modifier.padding(padding).padding(16.dp)) {
            OutlinedTextField(value = q, onValueChange = { q = it }, modifier = Modifier.fillMaxWidth(), label = { Text("Natural language query") })
            Button(onClick = { vm.search(bookId, q) }, modifier = Modifier.padding(vertical = 12.dp)) { Text("Retrieve") }
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.search(bookId, q) }) { results ->
                results.forEach { r ->
                    ElevatedCard(Modifier.fillMaxWidth().padding(bottom = 8.dp).clickable { onOpenUnit(r.unitId) }) {
                        Column(Modifier.padding(12.dp)) {
                            Text(r.unitTitle, fontWeight = FontWeight.Medium)
                            Text("similarity ${(r.similarity * 100).toInt()}%")
                            Text(r.chunkExcerpt)
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TaleemScreen(bookId: String, minutes: Int, onOpenUnit: (String) -> Unit, onBack: () -> Unit, vm: TaleemViewModel = hiltViewModel()) {
    LaunchedEffect(bookId, minutes) { vm.load(bookId, minutes) }
    Scaffold(topBar = {
        TopAppBar(title = { Text("$minutes-minute Ta'leem") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Box(Modifier.padding(padding)) {
            StateScaffold(vm.state.collectAsState().value, onRetry = { vm.load(bookId, minutes) }) { sel ->
                LazyColumn(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    item { Text("Approx ${sel.approximateSeconds / 60} min") }
                    items(sel.units) { u ->
                        ElevatedCard(Modifier.fillMaxWidth().clickable { onOpenUnit(u.id) }) {
                            Text(u.title, Modifier.padding(14.dp))
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(onBack: () -> Unit) {
    Scaffold(topBar = {
        TopAppBar(title = { Text("Settings") }, navigationIcon = { TextButton(onClick = onBack) { Text("Back") } })
    }) { padding ->
        Column(Modifier.padding(padding).padding(16.dp)) {
            Text("Online-first client. Local storage holds preferences and last reading location only.")
            Text("Books are data — this app never hardcodes a specific book.")
        }
    }
}
