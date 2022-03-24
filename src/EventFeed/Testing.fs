namespace EventFeed.Testing

open EventFeed
open EventFeed.Producer.Abstractions

type InMemoryEventFeedReader(eventPerPage: int,  evs: (FeedEvent seq)) =

    let events = if Seq.isEmpty evs then ResizeArray() else ResizeArray(evs)

    new(eventPerPage: int) = new InMemoryEventFeedReader(eventPerPage, Seq.empty)

    member this.AddEvent ev = events.Add(ev)
    member this.AddEvents evs = events.AddRange(evs)

    interface IEventFeedReader with
        member this.EventNumbers() = {
            EventCount = events.Count
            EventsPerPage = eventPerPage
        }
        member this.ReadPage page = 
            events |> Seq.skip (eventPerPage * (page - 1)) |> Seq.truncate eventPerPage

        member this.Dispose() = ()