using Azure.Identity;
using MCPostProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using System.Text;
using System.Text.Json;

namespace MCPostProcessor.Services;

/// <summary>
/// Service for sending email notifications using Microsoft Graph API
/// </summary>
public class EmailService : IEmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly GraphServiceClient _graphClient;

    public EmailService(IOptions<AppSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var clientSecretCredential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ApplicationId,
            _settings.ClientSecret
        );

        _graphClient = new GraphServiceClient(clientSecretCredential);
    }

    /// <summary>
    /// Sends an email notification with JSON data attached
    /// </summary>
    public async Task SendNotificationEmailAsync(List<TransformedPost> transformedPosts, string sharePointUrl)
    {
        try
        {
            _logger.LogInformation("Sending email notification to {Recipient}", _settings.EmailRecipient);

            // Create JSON attachment
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonContent = JsonSerializer.Serialize(transformedPosts, jsonOptions);
            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);
            var base64Content = Convert.ToBase64String(contentBytes);

            var fileName = $"MCPosts_{DateTime.UtcNow:yyyy-MM-dd}.json";

            // Build email message
            var emailBody = BuildEmailBody(transformedPosts.Count, sharePointUrl);

            var message = new Message
            {
                Subject = $"Message Center Posts - {DateTime.UtcNow:yyyy-MM-dd}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = emailBody
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = _settings.EmailRecipient
                        }
                    }
                },
                Attachments = new List<Attachment>
                {
                    new FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = fileName,
                        ContentType = "application/json",
                        ContentBytes = contentBytes
                    }
                }
            };

            var requestBody = new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            };

            // Send email using the application's identity
            // Note: The app needs Mail.Send permission and the mailbox must exist
            await _graphClient.Users[_settings.EmailRecipient].SendMail.PostAsync(requestBody);

            _logger.LogInformation("Email notification sent successfully to {Recipient}", _settings.EmailRecipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
            throw;
        }
    }

    /// <summary>
    /// Builds the HTML body for the notification email
    /// </summary>
    private string BuildEmailBody(int postCount, string sharePointUrl)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><body>");
        html.AppendLine("<h2>Message Center Posts Report</h2>");
        html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        html.AppendLine($"<p>Total posts found: <strong>{postCount}</strong></p>");
        
        if (!string.IsNullOrEmpty(sharePointUrl))
        {
            html.AppendLine($"<p>SharePoint file: <a href=\"{sharePointUrl}\">{sharePointUrl}</a></p>");
        }
        
        html.AppendLine("<p>The Message Center posts data is attached to this email as a JSON file.</p>");
        html.AppendLine("<p>Filter criteria: Posts containing 'Copilot' or 'Agent' in the title from the last 90 days.</p>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }
}
