
using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace HackerNewsBestStories.Api.Services.Interfaces;

public interface IHackerNewsService
{
    Task<IEnumerable<Story>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
}

