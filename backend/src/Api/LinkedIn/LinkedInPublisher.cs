using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Domain;

namespace Api.LinkedIn;

public sealed class LinkedInPublisher(HttpClient httpClient)
{
    /// <summary>
    /// Publish a text-only post to LinkedIn.
    /// </summary>
    public async Task PublishTextAsync(LinkedInConnection connection, ScheduledPost post, CancellationToken cancellationToken)
    {
        ValidateMemberUrn(connection);

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

        await SendUgcPostAsync(connection.AccessToken, payload, cancellationToken);
    }

    /// <summary>
    /// Publish a post with one or more images to LinkedIn.
    /// Each image must be registered, uploaded as binary, then referenced in the ugcPost.
    /// </summary>
    public async Task PublishWithMediaAsync(
        LinkedInConnection connection,
        ScheduledPost post,
        IReadOnlyList<MediaPayload> mediaPayloads,
        CancellationToken cancellationToken)
    {
        ValidateMemberUrn(connection);

        if (mediaPayloads.Count == 0)
        {
            // Fall back to text-only if no media provided
            await PublishTextAsync(connection, post, cancellationToken);
            return;
        }

        var mediaElements = new List<object>();

        foreach (var media in mediaPayloads)
        {
            // Step 1: Register the upload
            var registerPayload = new
            {
                registerUploadRequest = new
                {
                    owner = connection.MemberUrn,
                    recipes = new[] { "urn:li:digitalmediaRecipe:feedshare-image" },
                    serviceRelationships = new[]
                    {
                        new
                        {
                            identifier = "urn:li:userGeneratedContent",
                            relationshipType = "OWNER"
                        }
                    }
                }
            };

            var registerRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/assets?action=registerUpload")
            {
                Content = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json")
            };
            registerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

            using var registerResponse = await httpClient.SendAsync(registerRequest, cancellationToken);
            var registerBody = await registerResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!registerResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"LinkedIn registerUpload failed ({(int)registerResponse.StatusCode}): {registerBody}");
            }

            using var registerDoc = JsonDocument.Parse(registerBody);
            var valueRoot = registerDoc.RootElement.GetProperty("value");
            var uploadUrl = valueRoot
                .GetProperty("uploadMechanism")
                .GetProperty("com.linkedin.digitalmedia.uploading.MediaUploadHttpRequest")
                .GetProperty("uploadUrl")
                .GetString()!;
            var asset = valueRoot.GetProperty("asset").GetString()!;

            // Step 2: Upload the binary image
            using var uploadContent = new StreamContent(media.Content);
            uploadContent.Headers.ContentType = new MediaTypeHeaderValue(media.ContentType);
            var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = uploadContent
            };
            uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

            using var uploadResponse = await httpClient.SendAsync(uploadRequest, cancellationToken);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"LinkedIn image upload failed ({(int)uploadResponse.StatusCode}): {uploadBody}");
            }

            // Step 3: Build media element for ugcPost
            mediaElements.Add(new
            {
                status = "READY",
                description = new { text = media.FileName },
                media = asset,
                title = new { text = media.FileName }
            });
        }

        // Step 4: Create the ugcPost with image media
        var ugcPayload = new
        {
            author = connection.MemberUrn,
            lifecycleState = "PUBLISHED",
            specificContent = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.ShareContent"] = new
                {
                    shareCommentary = new { text = post.TextContent },
                    shareMediaCategory = "IMAGE",
                    media = mediaElements
                }
            },
            visibility = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        };

        await SendUgcPostAsync(connection.AccessToken, ugcPayload, cancellationToken);
    }

    private async Task SendUgcPostAsync(string accessToken, object payload, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LinkedIn publish failed ({(int)response.StatusCode}): {content}");
        }
    }

    private static void ValidateMemberUrn(LinkedInConnection connection)
    {
        if (string.IsNullOrWhiteSpace(connection.MemberUrn))
        {
            throw new InvalidOperationException("LinkedIn member URN is missing. Set it before publishing.");
        }
    }
}

/// <summary>
/// Represents a media file payload ready for upload to LinkedIn.
/// </summary>
public sealed class MediaPayload : IDisposable
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }

    public void Dispose() => Content.Dispose();
}
