using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using EventFeed.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using EventFeed.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventFeed.AspNet.Tests.Integration
{
    internal class AHostBuilder
    {
        private int _eventsPerPage = 100;
        private List<FeedEvent> events = new List<FeedEvent>();

        public AHostBuilder SetEventsPerPage(int eventsPerPage)
        {
            this._eventsPerPage = eventsPerPage;
            return this;
        }

        public AHostBuilder With(params FeedEvent[] events)
        {
            this.events.AddRange(events);
            return this;
        }

        public Task<IHost> Build()
        {
            return new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddEventFeed(_ => new InMemoryEventFeedReader(_eventsPerPage, events));
                        })
                        .Configure(app =>
                        {
                            app.UseEventFeed();
                        });
                })
                .StartAsync();
        }
    }
    internal static class A
    {
        public static AHostBuilder Host => new AHostBuilder();
    }
}
