namespace EventFeed.Abstractions

open EventFeed

type EventNumbers = {
    EventCount : int64
    EventsPerPage : int32
}

type IEventFeedReader =
    abstract member EventNumbers: unit -> EventNumbers
    abstract member ReadPage: int -> FeedEvent seq