using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Middleware;
using HackerNewsBestStories.Api.Services;
using HackerNewsBestStories.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hacker News Best Stories API",
        Version = "v1",
        Description = "API for retrieving top stories from Hacker News",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Configure settings
builder.Services.Configure<HackerNewsSettings>(
    builder.Configuration.GetSection("HackerNews"));

// Add memory cache with size limits
builder.Services.AddMemoryCache(); // Sin SizeLimit

// Register HTTP client with retry policy
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>()
    .AddPolicyHandler(GetRetryPolicy());

// Add response caching
builder.Services.AddResponseCaching();

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseResponseCaching();

// Add rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Retry policy for HttpClient
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan.Seconds} seconds");
            });
}