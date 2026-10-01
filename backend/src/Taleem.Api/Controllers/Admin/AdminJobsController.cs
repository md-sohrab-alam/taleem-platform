using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taleem.Application.Services;

namespace Taleem.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/v1/jobs")]
public sealed class AdminJobsController : ControllerBase
{
    private readonly IAdminBookService _admin;

    public AdminJobsController(IAdminBookService admin) => _admin = admin;

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken ct = default)
    {
        var job = await _admin.GetJobAsync(jobId, ct);
        return job is null ? NotFound() : Ok(job);
    }
}
