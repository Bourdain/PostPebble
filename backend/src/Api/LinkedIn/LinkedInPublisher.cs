using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Domain;

namespace Api.LinkedIn;

public sealed class LinkedInPublisher(HttpClient httpClient)
{
    public async Task PublishTextAsync(LinkedInConnection connection, ScheduledPost post, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connection.MemberUrn))
        {
            throw new InvalidOperationException("LinkedIn member URN is missing. Set it before publishing.");
        }

        var payload = new
        {
            author = connection.MemberUrn,
            lifecycleState = "PUBLISHED",
            specificContent = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.ShareContent"] = new
                {
                    shareCommentary = new { text = post.TextContent },
                    shareMediaCategory = "NONE"
                }
            },
            visibility = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LinkedIn publish failed ({(int)response.StatusCode}): {content}");
        }
    }
}
