using System.Text.Json.Serialization;

namespace Models;

public class ConfigurationRequest
{
    public ConfigurationRequest()
    {
        
    }
    [JsonPropertyName("url")] public string url { get; init; }
}