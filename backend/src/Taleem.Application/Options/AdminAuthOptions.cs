namespace Taleem.Application.Options;

public sealed class AdminAuthOptions
{
    public const string SectionName = "AdminAuth";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "changeme-dev-only";
    public string JwtSigningKey { get; set; } = "DEV_ONLY_CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+";
    public string Issuer { get; set; } = "taleem-admin";
    public string Audience { get; set; } = "taleem-admin";
    public int TokenLifetimeMinutes { get; set; } = 480;
}
