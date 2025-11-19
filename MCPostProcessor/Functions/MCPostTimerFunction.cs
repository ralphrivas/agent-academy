using MCPostProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MCPostProcessor.Functions;

/// <summary>
/// Azure Function that processes Message Center posts on a daily schedule
/// </summary>
public class MCPostTimerFunction
{
    private readonly ILogger<MCPostTimerFunction> _logger;
    private readonly IGraphService _graphService;
    private readonly ISharePointService _sharePointService;
    private readonly IEmailService _emailService;

    public MCPostTimerFunction(
        ILogger<MCPostTimerFunction> logger,
        IGraphService graphService,
        ISharePointService sharePointService,
        IEmailService emailService)
    {
        _logger = logger;
        _graphService = graphService;
        _sharePointService = sharePointService;
        _emailService = emailService;
    }

    /// <summary>
    /// Timer-triggered function that runs daily at 1 AM EST (6 AM UTC)
    /// NCRONTAB expression: "0 0 6 * * *" = At 6:00 AM UTC every day
    /// Note: Azure Functions use UTC time. EST is UTC-5, so 1 AM EST = 6 AM UTC
    /// During EDT (Eastern Daylight Time), 1 AM EDT = 5 AM UTC
    /// </summary>
    [Function("MCPostTimerFunction")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("MCPostTimerFunction started at: {Time}", DateTime.UtcNow);

        try
        {
            // Step 1: Fetch Message Center posts from Microsoft Graph API
            _logger.LogInformation("Step 1: Fetching Message Center posts...");
            var posts = await _graphService.GetMessageCenterPostsAsync(90);

            if (posts.Count == 0)
            {
                _logger.LogWarning("No Message Center posts found matching the filter criteria");
                return;
            }

            // Step 2: Transform the data
            _logger.LogInformation("Step 2: Transforming {Count} posts...", posts.Count);
            var transformedPosts = _graphService.TransformPosts(posts);

            // Step 3: Upload to SharePoint
            _logger.LogInformation("Step 3: Uploading to SharePoint...");
            var fileName = $"MCPosts_{DateTime.UtcNow:yyyy-MM-dd_HHmmss}.json";
            var sharePointUrl = await _sharePointService.UploadJsonToSharePointAsync(transformedPosts, fileName);

            // Step 4: Send email notification
            _logger.LogInformation("Step 4: Sending email notification...");
            await _emailService.SendNotificationEmailAsync(transformedPosts, sharePointUrl);

            _logger.LogInformation("MCPostTimerFunction completed successfully. Processed {Count} posts", posts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Message Center posts");
            throw;
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextRun}", myTimer.ScheduleStatus.Next);
        }
    }
}
