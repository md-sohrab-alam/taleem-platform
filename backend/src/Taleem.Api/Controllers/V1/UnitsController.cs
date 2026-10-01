using Microsoft.AspNetCore.Mvc;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/units")]
public sealed class UnitsController : ControllerBase
{
    private readonly IPublicContentService _content;

    public UnitsController(IPublicContentService content) => _content = content;

    [HttpGet("{unitId:guid}")]
    public async Task<IActionResult> Get(Guid unitId, CancellationToken ct = default)
    {
        var unit = await _content.GetUnitAsync(unitId, ct);
        return unit is null ? NotFound() : Ok(unit);
    }
}
