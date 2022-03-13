namespace EventFeed

open System
open System.Diagnostics
open System.Text.Json

type FeedEvent = {
    EventId: Guid
    EventName: string
    EventSchemaVersion: Int16
    Payload: string
    SequenceNumber: int64
    SpanId: string
    CreatedAt: DateTimeOffset
    TraceId: string
}

type NewFeedEvent = {
    EventId: Guid
    EventName: string
    EventSchemaVersion: Int16
    Payload: string
    SpanId: string
    CreatedAt: DateTimeOffset
    TraceId: string
}

module FeedEvent =

    let private currentActivity() = 
        match Activity.Current with
        | null -> None
        | activity -> Some activity
    
    let private getTraceId() = 
        currentActivity() 
        |> Option.map (fun a -> a.TraceId) 
        |> Option.defaultWith ActivityTraceId.CreateRandom

    let private getSpanId() = 
        currentActivity() 
        |> Option.map (fun a -> a.SpanId) 
        |> Option.defaultWith ActivitySpanId.CreateRandom

    [<CompiledName("From")>]
    let from name payload time version traceId spanId =
        let v = defaultArg version 1s
        let tId = defaultArg traceId (getTraceId())
        let sId = defaultArg spanId (getSpanId())
        let t = defaultArg time (DateTimeOffset.UtcNow)
        {
            EventId = Guid.NewGuid()
            EventName = name
            EventSchemaVersion = v
            Payload = payload
            SpanId = sId.ToString()
            CreatedAt = t
            TraceId = tId.ToString()
        }

    [<CompiledName("WithDefaults")>]
    let withDefaults name payload = from name payload None None None None

    let private serialize o = JsonSerializer.Serialize(o)

    [<CompiledName("fromObject")>]
    let fromObj name (payload: obj) time version traceId spanId =
        from name (serialize payload) time version traceId spanId

module Page =
    
    [<CompiledName("TotalPages")>]
    let totalPages (eventCount : int64) (eventsPerPage : int) =
            let epp = eventsPerPage |> int64
            if (eventCount < eventsPerPage) then 1
            else
                let remainder = eventCount % epp
                let add = if remainder > 0 then 1 else 0
                let page = (int)(eventCount / epp) + add
                page
        

namespace EventFeed.Abstractions

open EventFeed

type EventNumbers = {
    EventCount : int64
    EventsPerPage : int32
}

type IEventFeedReader =
    abstract member EventNumbers: unit -> EventNumbers
    abstract member ReadPage: int -> FeedEvent seq