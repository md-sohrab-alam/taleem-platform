using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taleem.Application.DTOs;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/v1/books")]
public sealed class AdminBooksController : ControllerBase
{
    private readonly IAdminBookService _admin;

    public AdminBooksController(IAdminBookService admin) => _admin = admin;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(await _admin.ListBooksAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request, CancellationToken ct = default)
    {
        var book = await _admin.CreateBookAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { bookId = book.Id }, book);
    }

    [HttpGet("{bookId:guid}")]
    public async Task<IActionResult> Get(Guid bookId, CancellationToken ct = default)
    {
        var book = await _admin.GetBookAsync(bookId, ct);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPut("{bookId:guid}")]
    public async Task<IActionResult> Update(Guid bookId, [FromBody] UpdateBookRequest request, CancellationToken ct = default)
    {
        var book = await _admin.UpdateBookAsync(bookId, request, ct);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost("{bookId:guid}/sources")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(
        Guid bookId,
        IFormFile file,
        [FromForm] string? sourceUrl,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File required" });

        await using var stream = file.OpenReadStream();
        try
        {
            var doc = await _admin.UploadSourceAsync(bookId, stream, file.FileName, file.ContentType, sourceUrl, ct);
            return Ok(doc);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{bookId:guid}/sources/{sourceDocumentId:guid}/ingest")]
    public async Task<IActionResult> StartIngestion(Guid bookId, Guid sourceDocumentId, CancellationToken ct = default)
    {
        try
        {
            var job = await _admin.StartIngestionAsync(bookId, sourceDocumentId, ct);
            return Accepted(job);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{bookId:guid}/jobs")]
    public async Task<IActionResult> Jobs(Guid bookId, CancellationToken ct = default)
        => Ok(await _admin.ListJobsAsync(bookId, ct));

    [HttpGet("{bookId:guid}/review")]
    public async Task<IActionResult> Review(Guid bookId, CancellationToken ct = default)
    {
        var review = await _admin.GetReviewAsync(bookId, ct);
        return review is null ? NotFound() : Ok(review);
    }

    [HttpPost("{bookId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid bookId, CancellationToken ct = default)
    {
        try
        {
            await _admin.ApproveBookAsync(bookId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{bookId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid bookId, CancellationToken ct = default)
    {
        try
        {
            await _admin.PublishBookAsync(bookId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{bookId:guid}")]
    public async Task<IActionResult> Delete(Guid bookId, [FromBody] DeleteBookConfirmation confirmation, CancellationToken ct = default)
    {
        try
        {
            await _admin.DeleteBookAsync(bookId, confirmation, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
