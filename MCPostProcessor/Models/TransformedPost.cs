using System.Text.Json.Serialization;

namespace MCPostProcessor.Models;

/// <summary>
/// Represents a transformed Message Center post matching Power Automate Select action output
/// </summary>
public class TransformedPost
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("StartDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("EndDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("LastModifiedDate")]
    public string LastModifiedDate { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("Severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("ActionRequiredBy")]
    public string ActionRequiredBy { get; set; } = string.Empty;

    [JsonPropertyName("Services")]
    public string Services { get; set; } = string.Empty;

    [JsonPropertyName("Tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("IsMajorChange")]
    public bool IsMajorChange { get; set; }

    [JsonPropertyName("Body")]
    public string Body { get; set; } = string.Empty;
}
