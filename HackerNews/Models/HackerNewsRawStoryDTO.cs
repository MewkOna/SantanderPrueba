

using System.Text.Json.Serialization;

public class HackerNewsRawStoryDTO
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("by")]
    public string By { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("dead")]
    public bool Dead { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}
