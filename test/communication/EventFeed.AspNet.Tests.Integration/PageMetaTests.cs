using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using EventFeed.AspNetCore;
using System.Text.Json;

namespace EventFeed.AspNet.Tests.Integration
{
    public class PageMetaTests
    {
        [Fact]
        public async Task Meta_endpoint_is_available()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Meta_endpoint_is_available_with_slash()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task With_no_events_pages_count_is_1()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed");
            var pageMeta = JsonSerializer.Deserialize<PageMeta>(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, pageMeta.EventCount);
            Assert.Equal(1, pageMeta.Pages);
        }

        [Fact]
        public async Task With_no_events_head_link_equals_tail_link()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed");
            var pageMeta = JsonSerializer.Deserialize<PageMeta>(await response.Content.ReadAsStringAsync());
            Assert.Equal(pageMeta._links._head, pageMeta._links._tail);
        }
    }
}