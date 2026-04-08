namespace Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PostPebble";
    public string Audience { get; set; } = "PostPebble.Web";
    public string SigningKey { get; set; } = "change-this-dev-signing-key-with-32-plus-chars";
    public int ExpirationMinutes { get; set; } = 120;
}
