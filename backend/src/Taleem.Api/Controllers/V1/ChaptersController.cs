using Microsoft.AspNetCore.Mvc;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/chapters")]
public sealed class ChaptersController : ControllerBase
{
    private readonly IPublicContentService _content;

    public ChaptersController(IPublicContentService content) => _content = content;

    [HttpGet("{chapterId:guid}")]
    public async Task<IActionResult> Get(Guid chapterId, CancellationToken ct = default)
    {
        var chapter = await _content.GetChapterAsync(chapterId, ct);
        return chapter is null ? NotFound() : Ok(chapter);
    }

    [HttpGet("{chapterId:guid}/units")]
    public async Task<IActionResult> Units(Guid chapterId, CancellationToken ct = default)
        => Ok(await _content.GetChapterUnitsAsync(chapterId, ct));
}
