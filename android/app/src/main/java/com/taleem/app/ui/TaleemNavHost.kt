package com.taleem.app.ui

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.taleem.app.ui.screens.*

@Composable
fun TaleemNavHost() {
    val nav = rememberNavController()
    NavHost(navController = nav, startDestination = "splash") {
        composable("splash") { SplashScreen(onDone = { nav.navigate("library") { popUpTo("splash") { inclusive = true } } }) }
        composable("library") {
            LibraryScreen(
                onOpenBook = { id -> nav.navigate("book/$id") },
                onSettings = { nav.navigate("settings") }
            )
        }
        composable("book/{bookId}", arguments = listOf(navArgument("bookId") { type = NavType.StringType })) {
            val bookId = it.arguments?.getString("bookId")!!
            BookHomeScreen(
                bookId = bookId,
                onOpenChapter = { chapterId -> nav.navigate("chapter/$chapterId") },
                onOpenUnit = { unitId -> nav.navigate("read/$unitId") },
                onTopics = { nav.navigate("topics/$bookId") },
                onSearch = { nav.navigate("search/$bookId") },
                onSemantic = { nav.navigate("semantic/$bookId") },
                onTaleem = { minutes -> nav.navigate("taleem/$bookId/$minutes") },
                onBack = { nav.popBackStack() }
            )
        }
        composable("chapter/{chapterId}", arguments = listOf(navArgument("chapterId") { type = NavType.StringType })) {
            ChapterScreen(it.arguments!!.getString("chapterId")!!, onOpenUnit = { id -> nav.navigate("read/$id") }, onBack = { nav.popBackStack() })
        }
        composable("read/{unitId}", arguments = listOf(navArgument("unitId") { type = NavType.StringType })) {
            ReaderScreen(it.arguments!!.getString("unitId")!!, onBack = { nav.popBackStack() })
        }
        composable("topics/{bookId}", arguments = listOf(navArgument("bookId") { type = NavType.StringType })) {
            TopicsScreen(it.arguments!!.getString("bookId")!!, onOpenUnit = { id -> nav.navigate("read/$id") }, onBack = { nav.popBackStack() })
        }
        composable("search/{bookId}", arguments = listOf(navArgument("bookId") { type = NavType.StringType })) {
            SearchScreen(it.arguments!!.getString("bookId")!!, onOpenUnit = { id -> nav.navigate("read/$id") }, onBack = { nav.popBackStack() })
        }
        composable("semantic/{bookId}", arguments = listOf(navArgument("bookId") { type = NavType.StringType })) {
            SemanticSearchScreen(it.arguments!!.getString("bookId")!!, onOpenUnit = { id -> nav.navigate("read/$id") }, onBack = { nav.popBackStack() })
        }
        composable(
            "taleem/{bookId}/{minutes}",
            arguments = listOf(
                navArgument("bookId") { type = NavType.StringType },
                navArgument("minutes") { type = NavType.IntType }
            )
        ) {
            TaleemScreen(
                bookId = it.arguments!!.getString("bookId")!!,
                minutes = it.arguments!!.getInt("minutes"),
                onOpenUnit = { id -> nav.navigate("read/$id") },
                onBack = { nav.popBackStack() }
            )
        }
        composable("settings") { SettingsScreen(onBack = { nav.popBackStack() }) }
    }
}
