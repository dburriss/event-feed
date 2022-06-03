﻿namespace EventFeed.Producer.Caching

open EventFeed
open EventFeed.Producer.Abstractions
open Microsoft.Extensions.Caching.Memory
open System

/// Provides an in-memory caching layer that wraps an IEventFeedReader
type CachingEventFeedReader(innerReader : IEventFeedReader, cache : IMemoryCache, completePageExpirationTime : TimeSpan, lastPageExpirationTime : TimeSpan) =

    let isLastPage (pageNumber : int) =
        let eventNumbers = innerReader.EventNumbers()
        let totalPages = Paging.totalPages eventNumbers.EventCount eventNumbers.EventsPerPage
        pageNumber = totalPages

    let getCacheEntryOptions (pageNumber : int) =
        match isLastPage(pageNumber) with
        | true ->  new MemoryCacheEntryOptions(AbsoluteExpirationRelativeToNow = lastPageExpirationTime)
        // Would be nice if we can use Size here so completed pages only get evicted when cache reaches max size
        | false -> new MemoryCacheEntryOptions(AbsoluteExpirationRelativeToNow = completePageExpirationTime )

    new(innerReader : IEventFeedReader, cache : IMemoryCache) = new CachingEventFeedReader(innerReader, cache, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1))

    interface IEventFeedReader with

        member this.EventNumbers() = innerReader.EventNumbers()

        member this.ReadPage pageNumber =
            match cache.Get<seq<FeedEvent>>(pageNumber) with
            | null ->
                let page = innerReader.ReadPage(pageNumber)
                cache.Set(pageNumber, page, getCacheEntryOptions(pageNumber))
            | entry -> entry

        member this.Dispose() = innerReader.Dispose()