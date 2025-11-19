namespace MCPostProcessor.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Azure AD Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (Client) ID
    /// </summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint Site URL
    /// </summary>
    public string SharePointSiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email recipient for notifications
    /// </summary>
    public string EmailRecipient { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint document library name for storing JSON files
    /// </summary>
    public string SharePointLibraryName { get; set; } = "Shared Documents";

    /// <summary>
    /// Number of days to look back for Message Center posts
    /// </summary>
    public int DaysToLookBack { get; set; } = 90;
}
