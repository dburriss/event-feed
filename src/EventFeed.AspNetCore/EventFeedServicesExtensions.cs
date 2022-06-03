using EventFeed.Producer.Abstractions;
using EventFeed.Producer.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EventFeed.AspNetCore
{
    public static class EventFeedServicesExtensions
    {
        public static IServiceCollection AddEventFeed(this IServiceCollection serviceCollection, Action<EventFeedOptionsBuilder> optionsBuilder)
        {
            var builder = new EventFeedOptionsBuilder();
            var options = builder.Options;
            optionsBuilder.Invoke(builder);

            if (options.CacheEnabled)
            {
                // wrap the EventFeedReader in a caching layer
                serviceCollection.AddTransient<IEventFeedReader>((provider) => new CachingEventFeedReader(
                    options.EventFeedReader,
                    provider.GetRequiredService<IMemoryCache>(),
                    options.CacheCompletePageExpirationTime,
                    options.CacheLastPageExpirationTime
                ));
            }
            else
            {
                serviceCollection.AddTransient(x => options.EventFeedReader);
            }

            serviceCollection.AddTransient<EventFeedMiddleware>();

            return serviceCollection;
        }
    }
}