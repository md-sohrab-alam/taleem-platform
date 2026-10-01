package com.taleem.app.data.api

import com.taleem.app.data.dto.*
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface TaleemApi {
    @GET("api/v1/books")
    suspend fun getBooks(@Query("page") page: Int = 1, @Query("pageSize") pageSize: Int = 50): PagedBooksDto

    @GET("api/v1/books/{bookId}/home")
    suspend fun getBookHome(@Path("bookId") bookId: String): BookHomeDto

    @GET("api/v1/books/{bookId}/chapters")
    suspend fun getChapters(@Path("bookId") bookId: String): List<ChapterSummaryDto>

    @GET("api/v1/chapters/{chapterId}")
    suspend fun getChapter(@Path("chapterId") chapterId: String): ChapterDetailDto

    @GET("api/v1/units/{unitId}")
    suspend fun getUnit(@Path("unitId") unitId: String): TaleemUnitDetailDto

    @GET("api/v1/books/{bookId}/topics")
    suspend fun getTopics(@Path("bookId") bookId: String): List<TopicDto>

    @GET("api/v1/books/{bookId}/topics/{topicId}/units")
    suspend fun getTopicUnits(@Path("bookId") bookId: String, @Path("topicId") topicId: String): List<TaleemUnitSummaryDto>

    @GET("api/v1/books/{bookId}/taleem")
    suspend fun getTaleem(@Path("bookId") bookId: String, @Query("minutes") minutes: Int): TaleemSelectionDto

    @GET("api/v1/books/{bookId}/search")
    suspend fun search(@Path("bookId") bookId: String, @Query("q") q: String): List<KeywordSearchResultDto>

    @POST("api/v1/search/semantic")
    suspend fun semanticSearch(@Body body: SemanticSearchRequestDto): List<SemanticSearchResultDto>
}
