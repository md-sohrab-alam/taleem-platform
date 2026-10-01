using Microsoft.AspNetCore.Mvc;
using Taleem.Application.DTOs;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IPublicContentService _content;

    public SearchController(IPublicContentService content) => _content = content;

    [HttpPost("semantic")]
    public async Task<IActionResult> Semantic([FromBody] SemanticSearchRequest request, CancellationToken ct = default)
        => Ok(await _content.SemanticSearchAsync(request, ct));
}
