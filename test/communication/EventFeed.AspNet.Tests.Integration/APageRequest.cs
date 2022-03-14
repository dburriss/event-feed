using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using EventFeed.AspNetCore;
using System.Text.Json;

namespace EventFeed.AspNet.Tests.Integration
{
    public class APageRequest
    {
        [Fact]
        public async Task With_no_events_head_is_reachable()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed/page/1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task With_no_page_return_400()
        {
            using var host = await A.Host.Build();
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task With_no_events_page_2_return_400()
        {
            using var host = await A.Host.Build();
            var nonExistentPage = 2;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{nonExistentPage}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task With_bad_request_returns_links()
        {
            using var host = await A.Host.Build();
            var nonExistentPage = 2;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{nonExistentPage}");
            var badRequest = JsonSerializer.Deserialize<BadRequestContent>(await response.Content.ReadAsStringAsync());
            Assert.Equal("/api/event-feed", badRequest._links._meta);
            Assert.Equal("/api/event-feed/page/1", badRequest._links._head);
            Assert.Equal("/api/event-feed/page/1", badRequest._links._tail);
        }

        [Fact]
        public async Task With_no_events_head_is_empty()
        {
            using var host = await A.Host.Build();
            var page = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{page}");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.Equal(page, pageMeta.page);
            Assert.Empty(pageMeta.events);
        }

        [Fact]
        public async Task With_existing_events_returns_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var page = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{page}");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(pageMeta.events);
        }

        [Fact]
        public async Task With_not_full_page_has_no_next()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var page = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{page}");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.Empty(pageMeta._links._next);
        }

        [Fact]
        public async Task On_first_page_has_no_previous()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var page = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{page}");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.Empty(pageMeta._links._previous);
        }

        [Fact]
        public async Task On_2nd_page_previous_is_first()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/2");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.Equal("/api/event-feed/page/1", pageMeta._links._previous);
        }

        [Fact]
        public async Task With_2nd_page_has_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var page = 2;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{page}");
            var pageMeta = JsonSerializer.Deserialize<EventFeedPage>(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(pageMeta.events);
        }

        [Fact]
        public async Task With_2nd_page_has_consecutive_events_across_pages()
        {
            var pageLength = 10;
            var totalEvents = pageLength * 2;
            using var host = await A.Host.SetEventsPerPage(pageLength).WithRandomEvents(totalEvents).Build();
            var response1 = await host.GetTestClient().GetAsync($"/api/event-feed/page/1");
            var response2 = await host.GetTestClient().GetAsync($"/api/event-feed/page/2");
            var pageMeta1 = JsonSerializer.Deserialize<EventFeedPage>(await response1.Content.ReadAsStringAsync());
            var pageMeta2 = JsonSerializer.Deserialize<EventFeedPage>(await response2.Content.ReadAsStringAsync());
            var pg1FirstEv = pageMeta1.events[0];
            var pg1LastEv = pageMeta1.events[pageLength - 1];
            var pg2FirstEv = pageMeta2.events[0];
            var pg2LastEv = pageMeta2.events[pageLength - 1];

            Assert.Equal(1, pg1FirstEv.SequenceNumber);
            Assert.Equal(10, pg1LastEv.SequenceNumber);
            Assert.Equal(11, pg2FirstEv.SequenceNumber);
            Assert.Equal(20, pg2LastEv.SequenceNumber);
        }
    }
}