namespace EventFeed.Producer.Caching.Tests.Unit

open EventFeed
open EventFeed.Producer.Abstractions
open System

type InMemoryEventFeedReader(eventCount : int, eventsPerPage : int) =
    let createFeedEvent sequenceNumber = {
            EventId = Guid.NewGuid()
            EventName = "FakeEvent"
            EventSchemaVersion = 1s
            Payload = ""
            SequenceNumber = sequenceNumber
            SpanId = Telemetry.getTraceId().ToString()
            CreatedAt = (DateTimeOffset.UtcNow)
            TraceId = Telemetry.getTraceId().ToString()
        }

    let events = [ for i in 1 .. eventCount -> createFeedEvent i ]
    
    let mutable readPageCallCount = 0
    member this.ReadPageCallCount = readPageCallCount

    interface IEventFeedReader with
        member this.EventNumbers() = {
            EventCount = eventCount
            EventsPerPage = eventsPerPage
        }

        member this.ReadPage pageNumber = 
            readPageCallCount <- readPageCallCount + 1
            let startIndex = pageNumber * eventsPerPage
            let endIndex = startIndex + eventsPerPage - 1
            events[startIndex..endIndex]

        member this.Dispose() = ()

module LibraryTests =
    open Xunit
    open EventFeed.Producer.Caching
    open Microsoft.Extensions.Caching.Memory

    [<Fact>]
    let ``Returns uncached page from inner reader`` () =
        let cache = new MemoryCache(new MemoryCacheOptions())
        let innerReader = new InMemoryEventFeedReader(eventCount = 25, eventsPerPage = 5)
        let cachingReader = new CachingEventFeedReader(innerReader, cache) :> IEventFeedReader
        
        cachingReader.ReadPage(1) |> ignore

        Assert.Equal(1, innerReader.ReadPageCallCount)

    [<Fact>]
    let ``Returns cached page without calling inner reader`` () =
        let cache = new MemoryCache(new MemoryCacheOptions())
        let innerReader = new InMemoryEventFeedReader(eventCount = 25, eventsPerPage = 5)
        let cachingReader = new CachingEventFeedReader(innerReader, cache) :> IEventFeedReader
        
        cachingReader.ReadPage(1) |> ignore
        cachingReader.ReadPage(1) |> ignore

        Assert.Equal(1, innerReader.ReadPageCallCount)

    [<Fact>]
    let ``Returns the same page for cached and non-cached`` () =
        let cache = new MemoryCache(new MemoryCacheOptions())
        let innerReader = new InMemoryEventFeedReader(eventCount = 25, eventsPerPage = 5)
        let cachingReader = new CachingEventFeedReader(innerReader, cache) :> IEventFeedReader
        
        let uncachedPage = cachingReader.ReadPage(1) |> ignore
        let cachedPage = cachingReader.ReadPage(1) |> ignore

        Assert.Equal(1, innerReader.ReadPageCallCount)
        Assert.Equal(uncachedPage, cachedPage)