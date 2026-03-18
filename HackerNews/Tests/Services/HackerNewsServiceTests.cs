using FluentAssertions;
using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NPOI.SS.Formula.Functions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace HackerNewsBestStories.Tests.Services;

public class HackerNewsServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<HackerNewsService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly IOptions<HackerNewsSettings> _settings;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _loggerMock = new Mock<ILogger<HackerNewsService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _settings = Options.Create(new HackerNewsSettings
        {
            BaseUrl = "https://test.com/",
            BestStoriesEndpoint = "beststories.json",
            ItemEndpoint = "item/{0}.json",
            CacheDurationMinutes = 5
        });

        _service = new HackerNewsService(_httpClient, _settings, _loggerMock.Object, _cache);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnTopNStories()
    {
        var storyIds = new List<int> { 1, 2, 3, 4, 5 };
        SetupMockResponse("beststories.json", storyIds);

        var story1 = CreateRawStory(1, "Story 1", 100);
        var story2 = CreateRawStory(2, "Story 2", 200);
        SetupMockResponse("item/1.json", story1);
        SetupMockResponse("item/2.json", story2);

        
        var result = await _service.GetBestStoriesAsync(2);

       
        result.Should().HaveCount(2);
        result.First().Score.Should().Be(200);
    }

    private void SetupMockResponse<T>(string url, T response)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(url)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
    }

    private HackerNewsRawStoryDTO CreateRawStory(int id, string title, int score)
    {
        return new HackerNewsRawStoryDTO
        {
            Id = id,
            Title = title,
            Score = score,
            By = "author",
            Time = 1234567890,
            Url = $"http://example.com/{id}",
            Type = "story",
            Descendants = 10
        };
    }
}