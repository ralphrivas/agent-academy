using Azure.Identity;
using MCPostProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Globalization;

namespace MCPostProcessor.Services;

/// <summary>
/// Service for Microsoft Graph API operations
/// </summary>
public class GraphService : IGraphService
{
    private readonly AppSettings _settings;
    private readonly ILogger<GraphService> _logger;
    private readonly GraphServiceClient _graphClient;

    public GraphService(IOptions<AppSettings> settings, ILogger<GraphService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Create client credential using Azure AD app registration
        var clientSecretCredential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ApplicationId,
            _settings.ClientSecret
        );

        _graphClient = new GraphServiceClient(clientSecretCredential);
    }

    /// <summary>
    /// Fetches Message Center posts filtered for Copilot/Agent content from the last specified days
    /// </summary>
    public async Task<List<MessageCenterPost>> GetMessageCenterPostsAsync(int daysToLookBack)
    {
        return await RetryHelper.ExecuteWithRetryAsync(
            async () => await FetchMessageCenterPostsInternalAsync(daysToLookBack),
            maxRetries: 3,
            _logger,
            nameof(GetMessageCenterPostsAsync)
        );
    }

    /// <summary>
    /// Internal method to fetch Message Center posts
    /// </summary>
    private async Task<List<MessageCenterPost>> FetchMessageCenterPostsInternalAsync(int daysToLookBack)
    {
        try
        {
            var lookBackDate = DateTime.UtcNow.AddDays(-daysToLookBack);
            var lookBackDateString = lookBackDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

            _logger.LogInformation("Fetching Message Center posts from the last {Days} days (since {Date})", 
                daysToLookBack, lookBackDateString);

            // Build filter: (contains(title, 'Copilot') or contains(title, 'Agent')) and (startDateTime ge {90 days ago})
            var filter = $"(contains(title, 'Copilot') or contains(title, 'Agent')) and (startDateTime ge {lookBackDateString})";

            _logger.LogInformation("Using filter: {Filter}", filter);

            // Call Microsoft Graph API
            var response = await _graphClient.Admin.ServiceAnnouncement.Messages
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = filter;
                    requestConfiguration.QueryParameters.Top = 1000; // Get up to 1000 messages
                });

            if (response?.Value == null)
            {
                _logger.LogWarning("No Message Center posts found matching the filter");
                return new List<MessageCenterPost>();
            }

            _logger.LogInformation("Retrieved {Count} Message Center posts", response.Value.Count);

            // Convert to our model
            var posts = response.Value.Select(m => new MessageCenterPost
            {
                Id = m.Id ?? string.Empty,
                Title = m.Title ?? string.Empty,
                StartDateTime = m.StartDateTime?.DateTime ?? DateTime.MinValue,
                EndDateTime = m.EndDateTime?.DateTime,
                LastModifiedDateTime = m.LastModifiedDateTime?.DateTime ?? DateTime.MinValue,
                Category = m.Category?.ToString() ?? string.Empty,
                Severity = m.Severity?.ToString() ?? string.Empty,
                ActionRequiredByDateTime = m.ActionRequiredByDateTime?.DateTime,
                Services = m.Services?.ToList() ?? new List<string>(),
                Tags = m.Tags?.ToList() ?? new List<string>(),
                IsMajorChange = m.IsMajorChange ?? false,
                Body = new MessageBody
                {
                    ContentType = m.Body?.ContentType?.ToString() ?? string.Empty,
                    Content = m.Body?.Content ?? string.Empty
                }
            }).ToList();

            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Message Center posts from Microsoft Graph API");
            throw;
        }
    }

    /// <summary>
    /// Transforms Message Center posts to the format matching Power Automate Select action
    /// </summary>
    public List<TransformedPost> TransformPosts(List<MessageCenterPost> posts)
    {
        _logger.LogInformation("Transforming {Count} Message Center posts", posts.Count);

        var transformedPosts = posts.Select(post => new TransformedPost
        {
            Id = post.Id,
            Title = post.Title,
            StartDate = FormatDate(post.StartDateTime),
            EndDate = FormatDate(post.EndDateTime),
            LastModifiedDate = FormatDate(post.LastModifiedDateTime),
            Category = post.Category,
            Severity = post.Severity,
            ActionRequiredBy = FormatDate(post.ActionRequiredByDateTime),
            Services = string.Join(", ", post.Services),
            Tags = string.Join(", ", post.Tags),
            IsMajorChange = post.IsMajorChange,
            Body = post.Body.Content
        }).ToList();

        _logger.LogInformation("Successfully transformed {Count} posts", transformedPosts.Count);

        return transformedPosts;
    }

    /// <summary>
    /// Formats a date to dd-MM-yyyy format
    /// </summary>
    private string FormatDate(DateTime? date)
    {
        if (date == null || date == DateTime.MinValue)
        {
            return string.Empty;
        }

        return date.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
}
