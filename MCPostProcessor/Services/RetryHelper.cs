using Microsoft.Extensions.Logging;
using System.Net;

namespace MCPostProcessor.Services;

/// <summary>
/// Helper class for implementing retry logic with exponential backoff
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes an async operation with retry logic
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="logger">Logger for recording retry attempts</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>Result of the operation</returns>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        ILogger logger,
        string operationName)
    {
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (retryCount < maxRetries && IsTransientError(ex))
            {
                retryCount++;
                logger.LogWarning(ex,
                    "Transient error in {Operation}. Retry attempt {RetryCount} of {MaxRetries}. Waiting {Delay}s before retry.",
                    operationName, retryCount, maxRetries, delay.TotalSeconds);

                await Task.Delay(delay);

                // Exponential backoff: double the delay for next retry (1s, 2s, 4s, 8s, etc.)
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {Operation} after {RetryCount} retries", operationName, retryCount);
                throw;
            }
        }
    }

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried
    /// </summary>
    private static bool IsTransientError(Exception ex)
    {
        // Handle HTTP-related errors
        if (ex is HttpRequestException httpEx)
        {
            return true; // Most HTTP errors are transient
        }

        // Handle specific .NET exceptions that are typically transient
        if (ex is TimeoutException || 
            ex is TaskCanceledException ||
            ex is OperationCanceledException)
        {
            return true;
        }

        // Handle WebException (older .NET exception type)
        if (ex is WebException webEx)
        {
            var statusCode = (webEx.Response as HttpWebResponse)?.StatusCode;
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout ||
                   statusCode == HttpStatusCode.TooManyRequests;
        }

        // Check inner exception
        if (ex.InnerException != null)
        {
            return IsTransientError(ex.InnerException);
        }

        return false;
    }
}
