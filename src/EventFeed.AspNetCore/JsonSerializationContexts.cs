using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventFeed.AspNetCore.Serialization
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(PageMeta))]
    public partial class PageMetaSerializerContext : JsonSerializerContext 
    {
        public static string Serialize(PageMeta value) => JsonSerializer.Serialize(value, Default.PageMeta);
        public static PageMeta? Deserialize(string json) => JsonSerializer.Deserialize(json, Default.PageMeta);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(EventFeedPage))]
    public partial class EventFeedPageSerializerContext : JsonSerializerContext 
    {
        public static string Serialize(EventFeedPage value) => JsonSerializer.Serialize(value, Default.EventFeedPage);
        public static EventFeedPage? Deserialize(string json) => JsonSerializer.Deserialize(json, Default.EventFeedPage);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(ProblemDetails))]
    public partial class ProblemDetailsSerializerContext : JsonSerializerContext
    {
        public static string Serialize(ProblemDetails value) => JsonSerializer.Serialize(value, Default.ProblemDetails);
        public static ProblemDetails? Deserialize(string json) => JsonSerializer.Deserialize(json, Default.ProblemDetails);
    }
}
