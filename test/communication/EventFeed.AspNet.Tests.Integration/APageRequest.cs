using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using EventFeed.AspNetCore.Serialization;
using System;

namespace EventFeed.AspNet.Tests.Integration
{
    public class APageRequest
    {
        Func<int, string> pageUrl = page => $"/api/event-feed/pages/{page}";

        [Fact]
        public async Task With_no_events_head_is_reachable()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(pageUrl(1));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task With_no_page_return_307_temporary_redirect()
        {
            using var host = await A.Host.Build();
            var response = await host.GetTestClient().GetAsync($"/api/event-feed/pages/");
            Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        }

        [Fact]
        public async Task With_no_events_page_2_return_404()
        {
            using var host = await A.Host.Build();
            var nonExistentPage = 2;
            var response = await host.GetTestClient().GetAsync(pageUrl(nonExistentPage));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task With_invalid_page_number_returns_problemdetails()
        {
            using var host = await A.Host.Build();
            var response = await host.GetTestClient().GetAsync("/api/event-feed/pages/notanumber");
            
            var problemDetails = ProblemDetailsSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal("Bad request", problemDetails!.Title);
        }

        [Fact]
        public async Task With_no_events_head_is_empty()
        {
            using var host = await A.Host.Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync(pageUrl(pageNumber));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal(pageNumber, page!.PageNumber);
            Assert.Empty(page.Events);
        }

        [Fact]
        public async Task With_existing_events_returns_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync(pageUrl(pageNumber));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(page!.Events);
        }

        [Fact]
        public async Task With_not_full_page_has_no_next()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync(pageUrl(pageNumber));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Empty(page!.Links.Next.Href);
        }

        [Fact]
        public async Task On_first_page_has_no_previous()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(5).Build();
            var pageNumber = 1;
            var response = await host.GetTestClient().GetAsync(pageUrl(pageNumber));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Empty(page!.Links.Previous.Href);
        }

        [Fact]
        public async Task On_2nd_page_previous_is_first()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var response = await host.GetTestClient().GetAsync(pageUrl(2));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.Equal("/api/event-feed/pages/1", page!.Links.Previous);
        }

        [Fact]
        public async Task With_2nd_page_has_events()
        {
            using var host = await A.Host.SetEventsPerPage(10).WithRandomEvents(20).Build();
            var pageNumber = 2;
            var response = await host.GetTestClient().GetAsync(pageUrl(pageNumber));
            var page = EventFeedPageSerializerContext.Deserialize(await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(page!.Events);
        }

        [Fact]
        public async Task With_2nd_page_has_consecutive_events_across_pages()
        {
            var pageLength = 10;
            var totalEvents = pageLength * 2;
            using var host = await A.Host.SetEventsPerPage(pageLength).WithRandomEvents(totalEvents).Build();
            var response1 = await host.GetTestClient().GetAsync(pageUrl(1));
            var response2 = await host.GetTestClient().GetAsync(pageUrl(2));
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