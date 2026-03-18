// Services/HackerNewsService.cs
using System.Collections.Concurrent;
using System.Text.Json;
using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsBestStories.Api.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsService> _logger;
    private readonly HackerNewsSettings _settings;
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim _semaphore;

    static HackerNewsService()
    {
        _semaphore = new SemaphoreSlim(10, 10); // Limit concurrent requests
    }

    public HackerNewsService(
        HttpClient httpClient,
        IOptions<HackerNewsSettings> settings,
        ILogger<HackerNewsService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.Value.ClientTimeoutSeconds);

        _logger = logger;
        _settings = settings.Value;
        _cache = cache;
    }

    public async Task<IEnumerable<Story>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return Enumerable.Empty<Story>();

        // Try to get from cache first
        string cacheKey = $"best_stories_{count}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Story>? cachedStories))
        {
            _logger.LogInformation("Returning cached stories for count: {Count}", count);
            return cachedStories!;
        }

        // Fetch best story IDs
        var storyIds = await GetBestStoryIdsAsync(cancellationToken);
        if (!storyIds.Any())
            return Enumerable.Empty<Story>();

        // Take top N IDs (we'll fetch details for these)
        var topIds = storyIds.Take(count * 2).ToList(); // Fetch extra for filtering

        // Fetch story details in parallel with rate limiting
        var stories = await FetchStoriesInParallel(topIds, cancellationToken);

        // Filter valid stories, sort by score, take top N
        var result = stories
            .Where(s => s != null && !string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.Uri))
            .OrderByDescending(s => s!.Score)
            .Take(count)
            .ToList();

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.CacheDurationMinutes))
            .SetSlidingExpiration(TimeSpan.FromMinutes(1))
            .SetSize(_settings.LimitCacheSize); // Limit cache size

        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    private async Task<List<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "best_story_ids";

        // Try to get IDs from cache
        if (_cache.TryGetValue(cacheKey, out List<int>? cachedIds))
            return cachedIds!;

        try
        {
            var response = await _httpClient.GetAsync(_settings.BestStoriesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ids = await response.Content.ReadFromJsonAsync<List<int>>(cancellationToken);

            if (ids != null)
            {
                // Cache IDs for shorter duration as they change more frequently
                _cache.Set(cacheKey, ids, TimeSpan.FromMinutes(5));
                return ids;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching best story IDs");
        }

        return new List<int>();
    }

    private async Task<List<Story?>> FetchStoriesInParallel(List<int> storyIds, CancellationToken cancellationToken)
    {
        var stories = new ConcurrentBag<Story?>();
        var tasks = storyIds.Select(async id =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var story = await GetStoryByIdAsync(id, cancellationToken);
                stories.Add(story);
            }
            finally
            {
                _semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return stories.ToList();
    }

    private async Task<Story?> GetStoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        string cacheKey = $"story_{id}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out Story? cachedStory))
            return cachedStory;

        try
        {
            var endpoint = string.Format(_settings.ItemEndpoint, id);
            var rawStory = await _httpClient.GetFromJsonAsync<HackerNewsRawStoryDTO>(endpoint, cancellationToken);

            if (rawStory == null || rawStory.Dead || rawStory.Deleted || rawStory.Type != "story")
                return null;

            var story = new Story
            {
                Title = rawStory.Title,
                Uri = rawStory.Url ?? $"https://news.ycombinator.com/item?id={rawStory.Id}",// fallback uri 
                PostedBy = rawStory.By,
                Time = DateTimeOffset.FromUnixTimeSeconds(rawStory.Time).UtcDateTime,
                Score = rawStory.Score,
                CommentCount = rawStory.Descendants
            };

            // Cache individual story
            _cache.Set(cacheKey, story, TimeSpan.FromMinutes(_settings.CacheDurationMinutes));

            return story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story {StoryId}", id);
            return null;
        }
    }
}