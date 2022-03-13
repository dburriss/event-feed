using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EventFeed.Abstractions;
using System.Text.Json;

namespace EventFeed.AspNetCore
{
    public class MetaLinks
    {
        public MetaLinks(string _head, string _tail)
        {
            this._head = _head;
            this._tail = _tail;
        }

        public string _head { get; private set; }
        public string _tail { get; private set; }
    }

    public class Links
    {
        public Links(string _head, string _previous, string _self, string _next, string _tail)
        {
            this._head = _head;
            this._previous = _previous;
            this._self = _self;
            this._next = _next;
            this._tail = _tail;
        }

        public string _head { get; private set; }
        public string _previous { get; private set; }
        public string _self { get; private set; }
        public string _next { get; private set; }
        public string _tail { get; private set; }
    }

    public class PageMeta
    {
        public PageMeta(long eventCount, int eventsPerPage, MetaLinks _links)
        {
            EventCount = eventCount;
            EventsPerPage = eventsPerPage;
            Pages = Page.TotalPages(eventCount, eventsPerPage);
            this._links = _links;
        }

        public long EventCount { get; private set; }
        public int EventsPerPage { get; private set; }
        public int Pages { get; private set; }
        public MetaLinks _links { get; set; }
    }

    public class EventFeedMiddleware : IMiddleware
    {
        private readonly IEventFeedReader feedReader;

        public EventFeedMiddleware(IEventFeedReader feedReader)
        {
            this.feedReader = feedReader;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-6.0
            // https://andrewlock.net/accessing-route-values-in-endpoint-middleware-in-aspnetcore-3/
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.template.templatematcher.trymatch?view=aspnetcore-6.0

            var basePath = "/api/event-feed";
            var eventNumbers = feedReader.EventNumbers();
            var eventCount = eventNumbers.EventCount;
            var eventsPerPage = eventNumbers.EventsPerPage;

            if (IsEventFeedMetaRequest(context, basePath))
            {
                var head = HeadLink(basePath);
                var tail = TailLink(basePath, eventCount, eventsPerPage);
                var links = new MetaLinks(head, tail);
                var content = new PageMeta(eventCount, eventsPerPage, links);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(content));
            }
            else if (IsPage(context, basePath))
            {
                var pageNumber = 0;
                // TODO: check registered
                var events = feedReader.ReadPage(pageNumber);
                // TODO: write metrics
            }
            else
            {
                await next(context);
            }

        }


        private string HeadLink(string basePath)
        {
            return $"{basePath}/page/1";
        }

        private string TailLink(string basePath, long eventCount, int eventsPerPage)
        {
            if(eventCount < eventsPerPage)
            {
                return $"{basePath}/page/1";
            }
            int page = Page.TotalPages(eventCount, eventsPerPage);
            return $"{basePath}/page/{page}";
        }

        private static bool IsEventFeedMetaRequest(HttpContext context, string path)
        {
            var pathsMatch = (context?.Request.Path == PathString.FromUriComponent(path)) || (context?.Request.Path == PathString.FromUriComponent(path+"/"));
            return IsGetRequest(context) && pathsMatch;
        }
        private static bool IsPage(HttpContext context, string basePath)
        {
            var pagePath = basePath + "/page";
            var isPage = context?.Request.Path.StartsWithSegments(pagePath);
            return IsGetRequest(context) && (isPage ?? false);
        }

        private static bool IsGetRequest(HttpContext context)
        {
            return context?.Request.Method.ToUpper() == "GET";
        }
    }

    public static class EventFeedMiddlewareExtensions
    {
        public static IApplicationBuilder UseEventFeed(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EventFeedMiddleware>();
        }
    }

    public static class EventFeedServicesExtensions
    {
        public static IServiceCollection AddEventFeed(this IServiceCollection serviceCollection, Func<IServiceProvider,IEventFeedReader> factory)
        {
            serviceCollection.AddTransient(factory);
            serviceCollection.AddTransient<EventFeedMiddleware>();
            return serviceCollection;
        }
    }
}