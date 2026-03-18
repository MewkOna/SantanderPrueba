using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(
        IHackerNewsService hackerNewsService,
        ILogger<StoriesController> logger)
    {
        _hackerNewsService = hackerNewsService;
        _logger = logger;
    }

    /// <summary>
    /// Get the best n stories from Hacker News
    /// </summary>
    /// <param name="n">Number of top stories to return (1-200)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of top stories sorted by score</returns>
    /// <response code="200">Returns the list of top stories</response>
    /// <response code="400">If n is invalid</response>
    /// <response code="429">If rate limit exceeded</response>
    /// <response code="503">If Hacker News API is unavailable</response>
    [HttpGet("best/{n}")]
    [ProducesResponseType(typeof(IEnumerable<Story>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<Story>>> GetBestStories(
        [Range(1, 200, ErrorMessage = "n must be between 1 and 200")] int n,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching top {Count} stories", n);

            var stories = await _hackerNewsService.GetBestStoriesAsync(n, cancellationToken);

            if (!stories.Any())
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Hacker News API is currently unavailable");
            }

            return Ok(stories);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was cancelled");
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for top {Count} stories", n);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}