package com.taleem.app.data.local

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

private val Context.dataStore by preferencesDataStore("taleem_prefs")

@Singleton
class PrefsStore @Inject constructor(@ApplicationContext private val context: Context) {
    private val lastBookId = stringPreferencesKey("last_book_id")
    private val lastUnitId = stringPreferencesKey("last_unit_id")

    val lastBookIdFlow: Flow<String?> = context.dataStore.data.map { it[lastBookId] }
    val lastUnitIdFlow: Flow<String?> = context.dataStore.data.map { it[lastUnitId] }

    suspend fun setLastBook(bookId: String) {
        context.dataStore.edit { it[lastBookId] = bookId }
    }

    suspend fun setLastUnit(unitId: String) {
        context.dataStore.edit { it[lastUnitId] = unitId }
    }
}
