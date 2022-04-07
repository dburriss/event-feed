namespace EventFeed.Producer.Caching

open EventFeed
open EventFeed.Producer.Abstractions
open Microsoft.Extensions.Caching.Memory
open System

/// Provides an in-memory caching layer that wraps an IEventFeedReader
type CachingEventFeedReader(underlyingReader : IEventFeedReader, cache : IMemoryCache) =
    static let completePageExpirationTime = TimeSpan.FromMinutes(5)
    static let lastPageExpirationTime = TimeSpan.FromSeconds(1)
    
    let isLastPage (pageNumber : int) =
        let eventNumbers = underlyingReader.EventNumbers()
        let totalPages = Paging.totalPages eventNumbers.EventCount eventNumbers.EventsPerPage
        pageNumber = totalPages

    let getCacheEntryOptions (pageNumber : int) =
        match isLastPage(pageNumber) with
        | true ->  new MemoryCacheEntryOptions(AbsoluteExpirationRelativeToNow = lastPageExpirationTime)
        // Would be nice if we can use Size here so completed pages only get evicted when cache reaches max size
        | false -> new MemoryCacheEntryOptions(AbsoluteExpirationRelativeToNow = completePageExpirationTime )

    interface IEventFeedReader with

        member this.EventNumbers() = underlyingReader.EventNumbers()

        member this.ReadPage pageNumber =
            match cache.Get(pageNumber) with
            | :? seq<FeedEvent> as entry -> entry
            | null ->
                let page = underlyingReader.ReadPage(pageNumber)
                cache.Set(pageNumber, page, getCacheEntryOptions(pageNumber))
            | _ -> failwith("Unexpected cache response")

        member this.Dispose() = underlyingReader.Dispose()