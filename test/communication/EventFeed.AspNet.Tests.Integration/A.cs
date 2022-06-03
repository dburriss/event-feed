using EventFeed.AspNetCore;
using EventFeed.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventFeed.AspNet.Tests.Integration
{
    internal class AHostBuilder
    {
        private int _eventsPerPage = 100;
        private List<FeedEvent> _events = new List<FeedEvent>();

        public AHostBuilder SetEventsPerPage(int eventsPerPage)
        {
            this._eventsPerPage = eventsPerPage;
            return this;
        }

        public AHostBuilder With(params FeedEvent[] events)
        {
            this._events.AddRange(events);
            return this;
        }
        public AHostBuilder WithRandomEvents(int eventsToCreate)
        {
            for (int i = 0; i < eventsToCreate; i++)
            {
                var ev = new FeedEvent(
                    eventId: Guid.NewGuid(),
                    eventName: "test-event",
                    eventSchemaVersion: 1,
                    payload: "{ clicked: true }",
                    sequenceNumber: _events.Count + 1,
                    spanId: Telemetry.getSpanId().ToString(),
                    createdAt: DateTimeOffset.UtcNow,
                    traceId: Telemetry.getTraceId().ToString()
                );
                this.With(ev);
            }
            return this;
        }

        public Task<IHost> Build()
        {
            return new HostBuilder()
                .ConfigureWebHost(webBuilder => {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services => {
                            services.AddMemoryCache();
                            services.AddEventFeed(options => options.UseReader(new InMemoryEventFeedReader(_eventsPerPage, _events)));
                        })
                        .Configure(app => {
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
