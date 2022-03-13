using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using EventFeed.AspNetCore;
using System.Text.Json;

namespace EventFeed.AspNet.Tests.Integration
{
    public class PageTests
    {
        [Fact]
        public async Task With_no_events_head_is_reachable()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed/page/1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        //[Fact]
        public async Task With_no_events_head_is_empty()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync("/api/event-feed/page/1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}