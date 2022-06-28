using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EventFeed.Producer.Abstractions;
using System.Text.Json.Serialization;
using EventFeed.AspNetCore.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace EventFeed.AspNetCore
{
    public class Link
    {
        public Link(string href, bool templated = false, string? rel = null)
        {
            Href = href;
            Templated = templated;
            Rel = rel;
        }

        [JsonPropertyName("href")]
        public string Href { get; private set; }

        [JsonPropertyName("templated")]
        public bool Templated { get; private set; }

        [JsonPropertyName("rel")]
        public string? Rel { get; private set; }

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                Link other => this.Href == other.Href,
                string otherHref => this.Href == otherHref,
                null => false,
                _ => false
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Href);
        }

        public static implicit operator Link(string href) => new Link(href);
    }
    public class MetaLinks
    {
        public MetaLinks(Link meta, Link head, Link tail, Link page)
        {
            Meta = meta;
            Head = head;
            Tail = tail;
            Page = page;
        }

        [JsonPropertyName("meta")]
        public Link Meta { get; private set; }
        [JsonPropertyName("head")]
        public Link Head { get; private set; }
        [JsonPropertyName("tail")]
        public Link Tail { get; private set; }
        [JsonPropertyName("page")]
        public Link Page { get; private set; }
    }

    public class Links
    {
        public Links(Link meta, Link head, Link previous, Link self, Link next, Link tail, Link page)
        {
            Meta = meta;
            Head = head;
            Previous = previous;
            Self = self;
            Next = next;
            Tail = tail;
            Page = page;
        }

        [JsonPropertyName("meta")]
        public Link Meta { get; private set; }
        [JsonPropertyName("head")]
        public Link Head { get; private set; }
        [JsonPropertyName("previous")]
        public Link Previous { get; private set; }
        [JsonPropertyName("self")]
        public Link Self { get; private set; }
        [JsonPropertyName("next")]
        public Link Next { get; private set; }
        [JsonPropertyName("tail")]
        public Link Tail { get; private set; }
        [JsonPropertyName("page")]
        public Link Page { get; private set; }
    }

    public class PageMeta
    {
        public PageMeta(long eventCount, int eventsPerPage, int pageCount, MetaLinks links)
        {
            EventCount = eventCount;
            EventsPerPage = eventsPerPage;
            this.pageCount = pageCount;
            Links = links;
        }

        [JsonPropertyName("eventCount")]
        public long EventCount { get; private set; }
        [JsonPropertyName("eventsPerPage")]
        public int EventsPerPage { get; private set; }
        [JsonPropertyName("pageCount")]
        public int pageCount { get; private set; }
        [JsonPropertyName("_links")]
        public MetaLinks Links { get; set; }
    }

    public class EventFeedPage
    {
        public EventFeedPage(int pageNumber, FeedEvent[] events, Links links, bool isComplete)
        {
            PageNumber = pageNumber;
            Events = events;
            Links = links;
            IsComplete = isComplete;
        }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; private set; }
        [JsonPropertyName("events")]
        public FeedEvent[] Events { get; private set; }
        [JsonPropertyName("_links")]
        public Links Links { get; private set; }
        [JsonPropertyName("isComplete")]
        public bool IsComplete { get; private set; }
    }

    public class BadRequestContent
    {
        public BadRequestContent(string message, MetaLinks links) 
        {
            Message = message;
            Links = links;
        }

        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("_links")]
        public MetaLinks Links { get; set; }
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
            if(IsEventFeedRequest(context, basePath))
            {
                if (IsEventFeedMetaRequest(context, basePath))
                {
                    await MetaPageResponse(context, basePath, eventCount, eventsPerPage, totalPages);
                }
                else if (IsPageRequest(context, basePath))
                {
                    var segments = GetSegments(context);
                    if (IsMissingPageNumber(segments))
                    {
                        RedirectToFirstPage(context, basePath, totalPages);
                    }
                    else
                    {
                        string pageNumberString = FindPageNumberSegment(segments);
                        if (int.TryParse(pageNumberString, out var pageNumber))
                        {
                            if (pageNumber > totalPages)
                            {
                                await UnknownPageRequest(context, $"Page {pageNumber} is greater than the current total number of pages {totalPages}.");
                            }
                            else
                            {
                                await EventFeedPage(context, basePath, eventsPerPage, totalPages, pageNumber);
                            }
                        }
                        else
                        {
                            await BadRequest(context, $"{pageNumberString} is not a valid page number.");
                        }
                    }
                }
                else
                {
                    await BadRequest(context, $"Unknown resource.");
                }
            }
            else
            {
                await next(context);
            }
        }

        private async Task EventFeedPage(HttpContext context, string basePath, int eventsPerPage, int totalPages, int pageNumber)
        {
            var events = feedReader.ReadPage(pageNumber).ToArray();
            var headLink = HeadLink(basePath);
            var previousLink = PreviousLink(basePath, pageNumber);
            var selfLink = SelfLink(basePath, pageNumber);
            var nextLink = NextLink(basePath, pageNumber, totalPages);
            var tailLink = TailLink(basePath, totalPages);
            var pageLink = PageLink(basePath);
            var links = new Links(basePath, headLink, previousLink, selfLink, nextLink, tailLink, pageLink);
            var isComplete = events.Length == eventsPerPage;
            var content = new EventFeedPage(pageNumber, events, links, isComplete);
            context.Response.ContentType = "application/hal+json";
            await context.Response.WriteAsync(EventFeedPageSerializerContext.Serialize(content));
        }

        private static Link PreviousLink(string basePath, int pageNumber)
        {
            if(pageNumber > 1)
            {
                return $"{basePath}/pages/{pageNumber - 1}";
            }
            else
            {
                return String.Empty;
            }
        }

        private static Link SelfLink(string basePath, int pageNumber)
        {
            return $"{basePath}/pages/{pageNumber}";
        }

        private static Link NextLink(string basePath, int pageNumber, int totalPages)
        {
            var nextPage = pageNumber + 1;
            if(nextPage <= totalPages)
            {
                return $"{basePath}/pages/{nextPage}";
            }
            else
            {
                return string.Empty;
            }
        }

        private static Link PageLink(string basePath)
        {
            return new Link("{basePath}/pages/{pageNumber}", true);
        }

        private static string FindPageNumberSegment(string[] segments)
        {
            return segments[3];
            // TODO: search for page and increment
        }
        readonly static char[] separator = new char[] { '/' };
        private static string[] GetSegments(HttpContext context)
        {
            return context.Request.Path.Value.Split(separator, options: StringSplitOptions.RemoveEmptyEntries);
        }

        private static void RedirectToFirstPage(HttpContext context, string basePath, int totalPages)
        {
            var links = new MetaLinks(basePath, HeadLink(basePath), TailLink(basePath, totalPages), PageLink(basePath));
            context.Response.StatusCode = 307;
            context.Response.Headers["Location"] = $"{basePath}/pages/1";
        }

        private static async Task UnknownPageRequest(HttpContext context, string message)
        {
            var problemDetails = new ProblemDetails()
            {
                Title = "Unknown page requested",
                Detail = message,
                Instance = context.Request.Path,
                Status = 404,
                Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/404"
            };
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(ProblemDetailsSerializerContext.Serialize(problemDetails));
        }

        private static async Task BadRequest(HttpContext context, string message)
        {
            var status = 400;
            var problemDetails = new ProblemDetails()
            {
                Title = "Bad request",
                Detail = message,
                Instance = context.Request.Path,
                Status = status,
                Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400"
            };
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(ProblemDetailsSerializerContext.Serialize(problemDetails));
        }

        private async Task MetaPageResponse(HttpContext context, string basePath, long eventCount, int eventsPerPage, int totalPages)
        {
            var head = HeadLink(basePath);
            var tail = TailLink(basePath, totalPages);
            var page = PageLink(basePath);
            var links = new MetaLinks(basePath, head, tail, page);
            var content = new PageMeta(eventCount, eventsPerPage, totalPages, links);
            context.Response.ContentType = "application/hal+json";
            await context.Response.WriteAsync(PageMetaSerializerContext.Serialize(content));
        }

        private static bool IsMissingPageNumber(string[] segments)
        {
            return  segments.Length < 4;
        }

        private static Link HeadLink(string basePath)
        {
            return $"{basePath}/pages/1";
        }

        private static Link TailLink(string basePath, int totalPages)
        {
            return $"{basePath}/pages/{totalPages}";
        }

        private static bool IsEventFeedRequest(HttpContext context, string path)
        {
            var pathsMatch = (context.Request.Path.StartsWithSegments(path));
            return IsGetRequest(context) && pathsMatch;
        }

        private static bool IsEventFeedMetaRequest(HttpContext context, string path)
        {
            var pathsMatch = (context.Request.Path == PathString.FromUriComponent(path)) || (context.Request.Path == PathString.FromUriComponent(path + "/"));
            return IsGetRequest(context) && pathsMatch;
        }

        private static bool IsPageRequest(HttpContext context, string basePath)
        {
            var pagePath = basePath + "/pages";
            var isPage = context.Request.Path.StartsWithSegments(pagePath);
            return IsGetRequest(context) && isPage;
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