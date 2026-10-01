using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taleem.Application.DTOs;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/v1")]
public sealed class AdminReviewController : ControllerBase
{
    private readonly IAdminBookService _admin;

    public AdminReviewController(IAdminBookService admin) => _admin = admin;

    [HttpGet("sources/{sourceDocumentId:guid}/pages")]
    public async Task<IActionResult> Pages(Guid sourceDocumentId, CancellationToken ct = default)
        => Ok(await _admin.GetExtractedPagesAsync(sourceDocumentId, ct));

    [HttpGet("units/{unitId:guid}")]
    public async Task<IActionResult> GetUnit(Guid unitId, CancellationToken ct = default)
    {
        var unit = await _admin.GetUnitReviewAsync(unitId, ct);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPut("units/{unitId:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid unitId, [FromBody] UpdateUnitRequest request, CancellationToken ct = default)
    {
        var unit = await _admin.UpdateUnitAsync(unitId, request, ct);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPut("blocks/{blockId:guid}")]
    public async Task<IActionResult> UpdateBlock(Guid blockId, [FromBody] UpdateBlockRequest request, CancellationToken ct = default)
    {
        var block = await _admin.UpdateBlockAsync(blockId, request, ct);
        return block is null ? NotFound() : Ok(block);
    }

    [HttpPut("units/{unitId:guid}/topics")]
    public async Task<IActionResult> SetTopics(Guid unitId, [FromBody] SetUnitTopicsRequest request, CancellationToken ct = default)
    {
        await _admin.SetUnitTopicsAsync(unitId, request, ct);
        return NoContent();
    }

    [HttpPost("units/merge")]
    public async Task<IActionResult> Merge([FromBody] MergeUnitsRequest request, CancellationToken ct = default)
    {
        await _admin.MergeUnitsAsync(request, ct);
        return NoContent();
    }

    [HttpPost("units/split")]
    public async Task<IActionResult> Split([FromBody] SplitUnitRequest request, CancellationToken ct = default)
        => Ok(await _admin.SplitUnitAsync(request, ct));

    [HttpGet("topics")]
    public async Task<IActionResult> Topics(CancellationToken ct = default)
        => Ok(await _admin.ListTopicsAsync(ct));
}
