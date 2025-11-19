using MCPostProcessor.Models;

namespace MCPostProcessor.Services;

/// <summary>
/// Interface for SharePoint operations
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Uploads JSON data to SharePoint document library
    /// </summary>
    /// <param name="transformedPosts">List of transformed posts to save as JSON</param>
    /// <param name="fileName">Name of the file to create</param>
    /// <returns>SharePoint item URL</returns>
    Task<string> UploadJsonToSharePointAsync(List<TransformedPost> transformedPosts, string fileName);
}
