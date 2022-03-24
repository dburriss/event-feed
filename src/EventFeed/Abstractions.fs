namespace EventFeed.Producer.Abstractions

open EventFeed

type EventNumbers = {
    EventCount : int64
    EventsPerPage : int32
}

type IEventFeedReader =
    inherit System.IDisposable
    abstract member EventNumbers: unit -> EventNumbers
    abstract member ReadPage: int -> FeedEvent seq