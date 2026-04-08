namespace Api.LinkedIn;

public sealed class LinkedInOptions
{
    public const string SectionName = "LinkedIn";

    public string ClientId { get; set; } = string.Empty;
    public string PrimaryClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:8080/api/v1/integrations/linkedin/callback";
    public string FrontendSuccessRedirectUrl { get; set; } = "http://localhost:5173/integrations/linkedin/success";
    public string FrontendErrorRedirectUrl { get; set; } = "http://localhost:5173/integrations/linkedin/error";
    public string Scopes { get; set; } = "w_member_social";
    public int StateTtlMinutes { get; set; } = 10;
}
