using System.Text.Json.Serialization;

namespace EventFeed.AspNetCore.Serialization {
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(PageMeta))]
    internal partial class PageMetaSerializerContext : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(BadRequestContent))]
    internal partial class BadRequestContentSerializerContext : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(EventFeedPage))]
    internal partial class EventFeedPageSerializerContext : JsonSerializerContext { }
}
