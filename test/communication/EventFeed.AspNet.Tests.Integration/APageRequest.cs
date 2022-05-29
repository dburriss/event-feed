using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using EventFeed.AspNetCore.Serialization;

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
            
            var badRequest = BadRequestContentSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal("/api/event-feed", badRequest!.Links.Meta.Href);
            Assert.Equal("/api/event-feed/page/1", badRequest.Links.Head.Href);
            Assert.Equal("/api/event-feed/page/1", badRequest.Links.Tail.Href);
        }

        [Fact]
        public async Task With_no_events_head_is_empty()
        {
            using var host = await A.Host.Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{pageNumber}");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal(pageNumber, page!.PageNumber);
            Assert.Empty(page.Events);
        }

        [Fact]
        public async Task With_existing_events_returns_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{pageNumber}");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(page!.Events);
        }

        [Fact]
        public async Task With_not_full_page_has_no_next()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{pageNumber}");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Empty(page!.Links.Next.Href);
        }

        [Fact]
        public async Task On_first_page_has_no_previous()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{pageNumber}");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Empty(page!.Links.Previous.Href);
        }

        [Fact]
        public async Task On_2nd_page_previous_is_first()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/2");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal("/api/event-feed/page/1", page!.Links.Previous);
        }

        [Fact]
        public async Task With_2nd_page_has_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var pageNumber = 2;
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/page/{pageNumber}");
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(page!.Events);
        }

        [Fact]
        public async Task With_2nd_page_has_consecutive_events_across_pages()
        {
            var pageLength = 10;
            var totalEvents = pageLength * 2;
            using var host = await A.Host.SetEventsPerPage(pageLength).WithRandomEvents(totalEvents).Build();
            var response1 = await host.GetTestClient().GetAsync($"/api/event-feed/page/1");
            var response2 = await host.GetTestClient().GetAsync($"/api/event-feed/page/2");
            var page1 = EventFeedPageSerializerContext.Deserialize(await response1.Content.ReadAsStringAsync());
            var page2 = EventFeedPageSerializerContext.Deserialize(await response2.Content.ReadAsStringAsync());
            var pg1FirstEv = page1!.Events[0];
            var pg1LastEv = page1.Events[pageLength - 1];
            var pg2FirstEv = page2!.Events[0];
            var pg2LastEv = page2.Events[pageLength - 1];

            Assert.Equal(1, pg1FirstEv.SequenceNumber);
            Assert.Equal(10, pg1LastEv.SequenceNumber);
            Assert.Equal(11, pg2FirstEv.SequenceNumber);
            Assert.Equal(20, pg2LastEv.SequenceNumber);
        }
    }
}