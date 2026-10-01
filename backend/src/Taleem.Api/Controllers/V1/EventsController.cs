using Microsoft.AspNetCore.Mvc;
using Taleem.Application.DTOs;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IPublicContentService _content;

    public EventsController(IPublicContentService content) => _content = content;

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] AnalyticsEventRequest request, CancellationToken ct = default)
    {
        await _content.RecordEventAsync(request, ct);
        return Accepted();
    }
}
