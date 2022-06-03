using EventFeed.Producer.Abstractions;

namespace EventFeed.AspNetCore
{
    public class EventFeedOptions
    {
        public IEventFeedReader? EventFeedReader { get; set; }
        public int EventsPerPage { get; set; } = 100;
        public bool CacheEnabled { get; set; } = true;
        public TimeSpan CacheCompletePageExpirationTime = TimeSpan.FromMinutes(5);
        public TimeSpan CacheLastPageExpirationTime = TimeSpan.FromSeconds(1);
    }
}