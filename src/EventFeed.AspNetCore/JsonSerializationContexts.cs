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
    [JsonSerializable(typeof(BadRequestContent))]
    public partial class BadRequestContentSerializerContext : JsonSerializerContext 
    {
        public static string Serialize(BadRequestContent value) => JsonSerializer.Serialize(value, Default.BadRequestContent);
        public static BadRequestContent? Deserialize(string json) => JsonSerializer.Deserialize(json, Default.BadRequestContent);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(EventFeedPage))]
    public partial class EventFeedPageSerializerContext : JsonSerializerContext 
    {
        public static string Serialize(EventFeedPage value) => JsonSerializer.Serialize(value, Default.EventFeedPage);
        public static EventFeedPage? Deserialize(string json) => JsonSerializer.Deserialize(json, Default.EventFeedPage);
    }
}
