using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Taleem.Application.Options;

namespace Taleem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/v1/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly AdminAuthOptions _options;

    public AdminAuthController(IOptions<AdminAuthOptions> options) => _options = options.Value;

    public sealed record LoginRequest(string Username, string Password);
    public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!string.Equals(request.Username, _options.Username, StringComparison.Ordinal)
            || !string.Equals(request.Password, _options.Password, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.TokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: [new Claim(ClaimTypes.Name, request.Username), new Claim(ClaimTypes.Role, "Admin")],
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires));
    }
}
