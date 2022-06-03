using EventFeed.Producer.Abstractions;

namespace EventFeed.AspNetCore
{
    public class EventFeedOptionsBuilder
    {
        public EventFeedOptions Options { get; set; } = new EventFeedOptions();

        public EventFeedOptionsBuilder UseReader(IEventFeedReader eventFeedReader)
        {
            Options.EventFeedReader = eventFeedReader;
            return this;
        }

        public EventFeedOptionsBuilder UseCaching(bool enabled)
        {
            Options.CacheEnabled = enabled;
            return this;
        }
    }
}