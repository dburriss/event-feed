using Microsoft.AspNetCore.Builder;

namespace EventFeed.AspNetCore
{
    public static class EventFeedMiddlewareExtensions
    {
        public static IApplicationBuilder UseEventFeed(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EventFeedMiddleware>();
        }
    }
}