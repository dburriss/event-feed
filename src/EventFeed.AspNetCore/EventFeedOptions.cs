using EventFeed.Producer.Abstractions;

namespace EventFeed.AspNetCore
{
    public class EventFeedOptions
    {
        public bool UseCaching { get; set; } = true;
        public int EventsPerPage { get; set; } = 100;
        public IEventFeedReader? EventFeedReader { get; set; }
    }
}