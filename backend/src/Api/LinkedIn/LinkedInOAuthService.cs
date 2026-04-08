using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Api.LinkedIn;

public sealed class LinkedInOAuthService(HttpClient httpClient, IOptions<LinkedInOptions> optionsAccessor)
{
    private readonly LinkedInOptions _options = optionsAccessor.Value;

    public string BuildAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = _options.Scopes,
            ["state"] = state
        };

        var queryString = string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return $"https://www.linkedin.com/oauth/v2/authorization?{queryString}";
    }

    public async Task<LinkedInTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.PrimaryClientSecret
        };

        using var content = new FormUrlEncodedContent(payload);
        using var response = await httpClient.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LinkedIn token exchange failed ({(int)response.StatusCode}): {responseText}");
        }

        var token = JsonSerializer.Deserialize<LinkedInTokenResponse>(responseText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("LinkedIn token response missing access token.");
        }

        return token;
    }
}

public sealed record LinkedInTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string Scope
);
