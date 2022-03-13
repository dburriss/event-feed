namespace EventFeed.Testing

open EventFeed.Abstractions
open EventFeed

type InMemoryEventFeedReader(eventPerPage: int,  evs: (FeedEvent seq)) =

    let events = if Seq.isEmpty evs then ResizeArray() else ResizeArray(evs)

    new(eventPerPage: int) = InMemoryEventFeedReader(eventPerPage, Seq.empty)

    member this.AddEvent ev = events.Add(ev)
    member this.AddEvents evs = events.AddRange(evs)

    interface IEventFeedReader with
        member this.EventNumbers() = {
            EventCount = events.Count
            EventsPerPage = eventPerPage
        }
        member this.ReadPage page = 
            Seq.empty