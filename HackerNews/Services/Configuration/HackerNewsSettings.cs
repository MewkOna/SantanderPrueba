namespace HackerNewsBestStories.Api.Configuration;

public class HackerNewsSettings
{
    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";
    public string BestStoriesEndpoint { get; set; } = "beststories.json";
    public string ItemEndpoint { get; set; } = "item/{0}.json";
    public int CacheDurationMinutes { get; set; } = 5;
    public int MaxConcurrentRequests { get; set; } = 10;
    public int RetryCount { get; set; } = 3;
    public int ClientTimeoutSeconds { get; set; } = 30;

    public int LimitCacheSize { get; set; } = 1024 ;

 
}