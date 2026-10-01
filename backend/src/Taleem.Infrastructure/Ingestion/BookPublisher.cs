using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Persistence;

namespace Taleem.Infrastructure.Ingestion;

public sealed class BookPublisher : IBookPublisher
{
    private readonly TaleemDbContext _db;
    private readonly ILogger<BookPublisher> _logger;

    public BookPublisher(TaleemDbContext db, ILogger<BookPublisher> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PublishAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken)
                   ?? throw new InvalidOperationException("Book not found.");

        if (book.Status is not (ContentLifecycleStatus.Approved or ContentLifecycleStatus.NeedsReview or ContentLifecycleStatus.Draft))
        {
            // Allow republish from Approved primarily; NeedsReview blocked for safety unless Approved.
        }

        if (book.Status != ContentLifecycleStatus.Approved)
            throw new InvalidOperationException("Book must be APPROVED before publish.");

        var hasUnits = await _db.TaleemUnits.AnyAsync(u => u.BookId == bookId, cancellationToken);
        if (!hasUnits)
            throw new InvalidOperationException("Cannot publish a book with no TaleemUnits.");

        book.Status = ContentLifecycleStatus.Published;
        book.PublishedAt = DateTimeOffset.UtcNow;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        book.ContentVersion += 1;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Published book {BookId} slug {Slug}", book.Id, book.Slug);
    }
}
