using Microsoft.AspNetCore.TestHost;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EventFeed.AspNet.Tests.Integration
{
    /// <summary>
    /// These tests are to ensure the contact is as expected by the client.
    /// If you break this test, it means you need to revert or make changes to the clients.
    /// Remember that the client is being used in a distributed system, so the client will be upgraded seperately to the producer.
    /// Make incremental changes and use versioning for breaking changes.
    /// </summary>
    public class ClientContractTests
    {
        const string defaultRoute = "/api/event-feed";

        private static JsonElement CheckPropertyExists(JsonElement root, string name, string message)
        {
            var b = root.TryGetProperty(name, out JsonElement el);
            Assert.True(b, message);
            return el;
        }

        private static JsonElement CheckHrefExists(JsonElement root, string message)
        {
            var b = root.TryGetProperty("href", out JsonElement el);
            Assert.True(b, message);
            return el;
        }

        private static JsonElement CheckLinkExists(JsonElement root)
        {
            var linkName = "_links";
            return CheckPropertyExists(root, linkName, "`_links` must exist in the meta response.");
        }

        [Fact]
        public async Task Meta_is_at_default_and_returns_ok()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(defaultRoute);
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Page_returns_ok()
        {
            using var host = await A.Host.Build();
            var firstPageUrl = defaultRoute + "/pages/1";
            var response = await host.GetTestClient().GetAsync(firstPageUrl);
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Meta_has_links_meta()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(defaultRoute);
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement links = CheckLinkExists(root);
            var meta = CheckPropertyExists(links, "self", "`self` must exist in the _link data response.");
            CheckHrefExists(meta, "`href` must exist in the `meta` property object.");
        }

        [Fact]
        public async Task Meta_has_links_head()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(defaultRoute);
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement links = CheckLinkExists(root);
            var meta = CheckPropertyExists(links, "head", "`head` must exist in the _link data response.");
            CheckHrefExists(meta, "`href` must exist in the `head` property object.");
        }

        [Fact]
        public async Task Meta_has_links_tail()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(defaultRoute);
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement links = CheckLinkExists(root);
            var meta = CheckPropertyExists(links, "tail", "`tail` must exist in the _link data response.");
            CheckHrefExists(meta, "`href` must exist in the `tail` property object.");
        }

        [Fact]
        public async Task Meta_has_links_page()
        {
            using var host = await A.Host.Build();

            var response = await host.GetTestClient().GetAsync(defaultRoute);
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement links = CheckLinkExists(root);
            var meta = CheckPropertyExists(links, "page", "`page` must exist in the _link data response.");
            CheckHrefExists(meta, "`href` must exist in the `page` property object.");
        }
        
        const string defaultHeadRoute = "/api/event-feed/pages/1";
        
        [Fact]
        public async Task Page_has_next()
        {
            using var host = await A.Host.WithRandomEvents(1001).Build();

            var response = await host.GetTestClient().GetAsync(defaultHeadRoute);
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement links = CheckLinkExists(root);
            var next = CheckPropertyExists(links, "next", "`next` must exist in the _link data response.");
            CheckHrefExists(next, "`href` must exist in the `page` property object.");
        }

        // todo: Page contract
        // todo: Event contract
    }
}
