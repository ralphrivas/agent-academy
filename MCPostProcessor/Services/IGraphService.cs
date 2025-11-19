using MCPostProcessor.Models;

namespace MCPostProcessor.Services;

/// <summary>
/// Interface for Microsoft Graph API operations
/// </summary>
public interface IGraphService
{
    /// <summary>
    /// Fetches Message Center posts filtered for Copilot/Agent content from the last specified days
    /// </summary>
    /// <param name="daysToLookBack">Number of days to look back</param>
    /// <returns>List of Message Center posts</returns>
    Task<List<MessageCenterPost>> GetMessageCenterPostsAsync(int daysToLookBack);

    /// <summary>
    /// Transforms Message Center posts to the format matching Power Automate Select action
    /// </summary>
    /// <param name="posts">List of Message Center posts to transform</param>
    /// <returns>List of transformed posts</returns>
    List<TransformedPost> TransformPosts(List<MessageCenterPost> posts);
}
