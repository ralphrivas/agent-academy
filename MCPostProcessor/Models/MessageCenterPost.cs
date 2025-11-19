using System.Text.Json.Serialization;

namespace MCPostProcessor.Models;

/// <summary>
/// Represents a Message Center post from Microsoft Graph API
/// </summary>
public class MessageCenterPost
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("startDateTime")]
    public DateTime StartDateTime { get; set; }

    [JsonPropertyName("endDateTime")]
    public DateTime? EndDateTime { get; set; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime LastModifiedDateTime { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("actionRequiredByDateTime")]
    public DateTime? ActionRequiredByDateTime { get; set; }

    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = new List<string>();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();

    [JsonPropertyName("isMajorChange")]
    public bool IsMajorChange { get; set; }

    [JsonPropertyName("body")]
    public MessageBody Body { get; set; } = new MessageBody();
}

/// <summary>
/// Represents the body content of a message
/// </summary>
public class MessageBody
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response from Microsoft Graph Message Center API
/// </summary>
public class MessageCenterResponse
{
    [JsonPropertyName("@odata.context")]
    public string ODataContext { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public List<MessageCenterPost> Value { get; set; } = new List<MessageCenterPost>();
}
