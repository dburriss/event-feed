using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EventFeed.Abstractions;
using System.Text.Json;

namespace EventFeed.AspNetCore
{
    public class MetaLinks
    {
        public MetaLinks(string _meta, string _head, string _tail)
        {
            this._meta = _meta;
            this._head = _head;
            this._tail = _tail;
        }

        public string _meta { get; private set; }
        public string _head { get; private set; }
        public string _tail { get; private set; }
    }

    public class Links
    {
        public Links(string _meta, string _head, string _previous, string _self, string _next, string _tail)
        {
            this._meta = _meta;
            this._head = _head;
            this._previous = _previous;
            this._self = _self;
            this._next = _next;
            this._tail = _tail;
        }

        public string _meta { get; private set; }
        public string _head { get; private set; }
        public string _previous { get; private set; }
        public string _self { get; private set; }
        public string _next { get; private set; }
        public string _tail { get; private set; }
    }

    public class PageMeta
    {
        public PageMeta(long eventCount, int eventsPerPage, int pages, MetaLinks _links)
        {
            EventCount = eventCount;
            EventsPerPage = eventsPerPage;
            Pages = pages;
            this._links = _links;
        }

        public long EventCount { get; private set; }
        public int EventsPerPage { get; private set; }
        public int Pages { get; private set; }
        public MetaLinks _links { get; set; }
    }

    public class EventFeedPage
    {
        public EventFeedPage(int page, FeedEvent[] events, Links _links, bool isComplete)
        {
            this.page = page;
            this.events = events;
            this._links = _links;
            this.isComplete = isComplete;
        }

        public int page { get; private set; }
        public FeedEvent[] events { get; private set; }
        public Links _links { get; private set; }
        public bool isComplete { get; private set; }
    }

    public class BadRequestContent
    {
        public string message { get; set; }
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
            var totalPages = Paging.TotalPages(eventCount, eventsPerPage);
            // TODO: check registered
            // TODO: write metrics
            if (IsEventFeedMetaRequest(context, basePath))
            {
                await MetaPageResponse(context, basePath, eventCount, eventsPerPage, totalPages);
            }
            else if (IsPageRequest(context, basePath))
            {
                var segments = GetSegments(context);
                if (IsMissingPageNumber(segments))
                {
                    await BadRequest(context, basePath, totalPages, "No `page` number supplied.");
                }
                else
                {
                    string pageNumberString = FindPageNumberSegment(segments);
                    var pageNumber = 1;
                    if (int.TryParse(pageNumberString, out pageNumber))
                    {
                        if(pageNumber > totalPages)
                        {
                            await BadRequest(context, basePath, totalPages, $"Page {pageNumber} is greater than the current total number of pages {totalPages}.");
                        }
                        else
                        {
                            var events = feedReader.ReadPage(pageNumber).ToArray();
                            var headLink = HeadLink(basePath);
                            string previousLink = PreviousLink(basePath, pageNumber);
                            string selfLink = SelfLink(basePath, pageNumber);
                            string nextLink = NextLink(basePath, pageNumber, totalPages);
                            var tailLink = TailLink(basePath, totalPages);
                            var links = new Links(basePath, headLink, previousLink, selfLink, nextLink, tailLink);
                            var isComplete = events.Length == eventsPerPage;
                            var content = new EventFeedPage(pageNumber, events, links, isComplete);
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(JsonSerializer.Serialize(content));
                        }
                    }
                    else
                    {
                        await BadRequest(context, basePath, totalPages, $"{pageNumberString} is not a valid page.");
                    }
                }
            }
            else
            {
                await next(context);
            }
        }

        private static string PreviousLink(string basePath, int pageNumber)
        {
            if(pageNumber > 1)
            {
                return $"{basePath}/page/{pageNumber - 1}";
            }
            else
            {
                return String.Empty;
            }
        }

        private static string SelfLink(string basePath, int pageNumber)
        {
            return $"{basePath}/page/{pageNumber}";
        }

        private static string NextLink(string basePath, int pageNumber, int totalPages)
        {
            var nextPage = pageNumber + 1;
            if(nextPage <= totalPages)
            {
                return $"{basePath}/page/{nextPage}";
            }
            else
            {
                return string.Empty;
            }
        }

        private static string FindPageNumberSegment(string[] segments)
        {
            return segments[3];
            // TODO: search for page and increment
        }

        private static string[] GetSegments(HttpContext context)
        {
            return context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }

        private static async Task BadRequest(HttpContext context, string basePath, int totalPages, string message)
        {
            var content = new BadRequestContent
            {
                message = message,
                _links = new MetaLinks(basePath, HeadLink(basePath), TailLink(basePath, totalPages))
            };
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(content));
        }

        private async Task MetaPageResponse(HttpContext context, string basePath, long eventCount, int eventsPerPage, int totalPages)
        {
            var head = HeadLink(basePath);
            var tail = TailLink(basePath, totalPages);
            var links = new MetaLinks(basePath, head, tail);
            var content = new PageMeta(eventCount, eventsPerPage, totalPages, links);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(content));
        }

        private static bool IsMissingPageNumber(string[] segments)
        {
            return segments.Length < 4;
        }

        private static string HeadLink(string basePath)
        {
            return $"{basePath}/page/1";
        }

        private static string TailLink(string basePath, int totalPages)
        {
            return $"{basePath}/page/{totalPages}";
        }

        private static bool IsEventFeedMetaRequest(HttpContext context, string path)
        {
            var pathsMatch = (context?.Request.Path == PathString.FromUriComponent(path)) || (context?.Request.Path == PathString.FromUriComponent(path + "/"));
            return IsGetRequest(context) && pathsMatch;
        }
        private static bool IsPageRequest(HttpContext context, string basePath)
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
        public static IServiceCollection AddEventFeed(this IServiceCollection serviceCollection, Func<IServiceProvider, IEventFeedReader> factory)
        {
            serviceCollection.AddTransient(factory);
            serviceCollection.AddTransient<EventFeedMiddleware>();
            return serviceCollection;
        }
    }
}