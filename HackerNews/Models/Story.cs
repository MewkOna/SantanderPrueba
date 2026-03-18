// Models/Story.cs - API Response Model
using System.Text.Json.Serialization;

namespace HackerNewsBestStories.Api.Models;

public class Story
{
    public string Title { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string PostedBy { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public int Score { get; set; }
    public int CommentCount { get; set; }
}
