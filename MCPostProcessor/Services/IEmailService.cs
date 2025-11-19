using MCPostProcessor.Models;

namespace MCPostProcessor.Services;

/// <summary>
/// Interface for email operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email notification with JSON data attached
    /// </summary>
    /// <param name="transformedPosts">List of transformed posts to include in email</param>
    /// <param name="sharePointUrl">URL of the uploaded SharePoint file</param>
    /// <returns>Task representing the async operation</returns>
    Task SendNotificationEmailAsync(List<TransformedPost> transformedPosts, string sharePointUrl);
}
