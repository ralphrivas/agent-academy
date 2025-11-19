using Azure.Identity;
using MCPostProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System.Text;
using System.Text.Json;

namespace MCPostProcessor.Services;

/// <summary>
/// Service for SharePoint operations using Microsoft Graph API
/// </summary>
public class SharePointService : ISharePointService
{
    private readonly AppSettings _settings;
    private readonly ILogger<SharePointService> _logger;
    private readonly GraphServiceClient _graphClient;

    public SharePointService(IOptions<AppSettings> settings, ILogger<SharePointService> logger)
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
    /// Uploads JSON data to SharePoint document library
    /// </summary>
    public async Task<string> UploadJsonToSharePointAsync(List<TransformedPost> transformedPosts, string fileName)
    {
        try
        {
            _logger.LogInformation("Uploading {Count} posts to SharePoint as {FileName}", 
                transformedPosts.Count, fileName);

            // Serialize the data to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonContent = JsonSerializer.Serialize(transformedPosts, jsonOptions);
            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);

            // Extract site details from URL
            // Expected format: https://{tenant}.sharepoint.com/sites/{siteName}
            var uri = new Uri(_settings.SharePointSiteUrl);
            var hostname = uri.Host;
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathSegments.Length < 2 || pathSegments[0] != "sites")
            {
                throw new InvalidOperationException($"Invalid SharePoint site URL format: {_settings.SharePointSiteUrl}");
            }

            var siteName = pathSegments[1];

            _logger.LogInformation("Uploading to SharePoint site: {Hostname}, Site: {SiteName}", 
                hostname, siteName);

            // Get the site ID
            var site = await _graphClient.Sites[$"{hostname}:/sites/{siteName}"].GetAsync();
            
            if (site?.Id == null)
            {
                throw new InvalidOperationException($"Could not find SharePoint site: {_settings.SharePointSiteUrl}");
            }

            _logger.LogInformation("Found SharePoint site with ID: {SiteId}", site.Id);

            // Get the drive (document library)
            var drives = await _graphClient.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => 
                d.Name?.Equals(_settings.SharePointLibraryName, StringComparison.OrdinalIgnoreCase) == true);

            if (drive == null)
            {
                _logger.LogWarning("Document library '{LibraryName}' not found, using default drive", 
                    _settings.SharePointLibraryName);
                drive = drives?.Value?.FirstOrDefault();
            }

            if (drive?.Id == null)
            {
                throw new InvalidOperationException("Could not find a suitable document library in SharePoint");
            }

            _logger.LogInformation("Using document library: {DriveName} (ID: {DriveId})", 
                drive.Name, drive.Id);

            // Upload the file
            using var stream = new MemoryStream(contentBytes);
            var uploadSession = await _graphClient.Drives[drive.Id]
                .Items["root"]
                .ItemWithPath(fileName)
                .Content
                .PutAsync(stream);

            // Get the uploaded item to retrieve the URL
            var uploadedItem = await _graphClient.Drives[drive.Id]
                .Items["root"]
                .ItemWithPath(fileName)
                .GetAsync();

            var itemUrl = uploadedItem?.WebUrl ?? string.Empty;
            
            _logger.LogInformation("Successfully uploaded file to SharePoint: {Url}", itemUrl);

            return itemUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading JSON to SharePoint");
            throw;
        }
    }
}
