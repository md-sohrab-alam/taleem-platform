using Microsoft.AspNetCore.Mvc;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/books")]
public sealed class BooksController : ControllerBase
{
    private readonly IPublicContentService _content;

    public BooksController(IPublicContentService content) => _content = content;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _content.GetPublishedBooksAsync(page, pageSize, ct));

    [HttpGet("{idOrSlug}")]
    public async Task<IActionResult> Get(string idOrSlug, CancellationToken ct = default)
    {
        var book = await _content.GetBookAsync(idOrSlug, ct);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpGet("{bookId:guid}/home")]
    public async Task<IActionResult> Home(Guid bookId, [FromHeader(Name = "X-User-Key")] string? userKey, CancellationToken ct = default)
    {
        var home = await _content.GetBookHomeAsync(bookId, userKey, ct);
        return home is null ? NotFound() : Ok(home);
    }

    [HttpGet("{bookId:guid}/sections")]
    public async Task<IActionResult> Sections(Guid bookId, CancellationToken ct = default)
        => Ok(await _content.GetSectionsAsync(bookId, ct));

    [HttpGet("{bookId:guid}/chapters")]
    public async Task<IActionResult> Chapters(Guid bookId, CancellationToken ct = default)
        => Ok(await _content.GetChaptersAsync(bookId, ct));

    [HttpGet("{bookId:guid}/topics")]
    public async Task<IActionResult> Topics(Guid bookId, CancellationToken ct = default)
        => Ok(await _content.GetTopicsAsync(bookId, ct));

    [HttpGet("{bookId:guid}/topics/{topicId:guid}/units")]
    public async Task<IActionResult> TopicUnits(Guid bookId, Guid topicId, CancellationToken ct = default)
        => Ok(await _content.GetUnitsByTopicAsync(bookId, topicId, ct));

    [HttpGet("{bookId:guid}/taleem")]
    public async Task<IActionResult> Taleem(Guid bookId, [FromQuery] int minutes = 10, CancellationToken ct = default)
    {
        var result = await _content.GetTaleemByDurationAsync(bookId, minutes, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{bookId:guid}/search")]
    public async Task<IActionResult> Search(Guid bookId, [FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _content.KeywordSearchAsync(bookId, q ?? string.Empty, limit, ct));
}
