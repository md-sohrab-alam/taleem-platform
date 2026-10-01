using Taleem.Application.DTOs;

namespace Taleem.Application.Services;

public interface IAdminBookService
{
    Task<IReadOnlyList<AdminBookDto>> ListBooksAsync(CancellationToken ct = default);
    Task<AdminBookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<AdminBookDto?> GetBookAsync(Guid bookId, CancellationToken ct = default);
    Task<AdminBookDto?> UpdateBookAsync(Guid bookId, UpdateBookRequest request, CancellationToken ct = default);
    Task<SourceDocumentDto> UploadSourceAsync(Guid bookId, Stream content, string originalFilename, string contentType, string? sourceUrl, CancellationToken ct = default);
    Task<IngestionJobDto> StartIngestionAsync(Guid bookId, Guid sourceDocumentId, CancellationToken ct = default);
    Task<IngestionJobDto?> GetJobAsync(Guid jobId, CancellationToken ct = default);
    Task<IReadOnlyList<IngestionJobDto>> ListJobsAsync(Guid bookId, CancellationToken ct = default);
    Task<ReviewBookDto?> GetReviewAsync(Guid bookId, CancellationToken ct = default);
    Task<IReadOnlyList<ExtractedPageDto>> GetExtractedPagesAsync(Guid sourceDocumentId, CancellationToken ct = default);
    Task<AdminUnitReviewDto?> GetUnitReviewAsync(Guid unitId, CancellationToken ct = default);
    Task<AdminUnitReviewDto?> UpdateUnitAsync(Guid unitId, UpdateUnitRequest request, CancellationToken ct = default);
    Task<ContentBlockDto?> UpdateBlockAsync(Guid blockId, UpdateBlockRequest request, CancellationToken ct = default);
    Task SetUnitTopicsAsync(Guid unitId, SetUnitTopicsRequest request, CancellationToken ct = default);
    Task MergeUnitsAsync(MergeUnitsRequest request, CancellationToken ct = default);
    Task<AdminUnitReviewDto?> SplitUnitAsync(SplitUnitRequest request, CancellationToken ct = default);
    Task ApproveBookAsync(Guid bookId, CancellationToken ct = default);
    Task PublishBookAsync(Guid bookId, CancellationToken ct = default);
    Task DeleteBookAsync(Guid bookId, DeleteBookConfirmation confirmation, CancellationToken ct = default);
    Task<IReadOnlyList<TopicDto>> ListTopicsAsync(CancellationToken ct = default);
}
