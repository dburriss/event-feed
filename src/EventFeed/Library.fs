namespace EventFeed

open System

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

module Paging =
    
    [<CompiledName("TotalPages")>]
    let totalPages (eventCount : int64) (eventsPerPage : int) =
            let epp = eventsPerPage |> int64
            if (eventCount < eventsPerPage) then 1
            else
                let remainder = eventCount % epp
                let add = if remainder > 0 then 1 else 0
                let page = (int)(eventCount / epp) + add
                page

module Telemetry =
    open System.Diagnostics

    let currentActivity() = 
        match Activity.Current with
        | null -> None
        | activity -> Some activity
    
    let getTraceId() = 
        currentActivity() 
        |> Option.map (fun a -> a.TraceId) 
        |> Option.defaultWith ActivityTraceId.CreateRandom

    let getSpanId() = 
        currentActivity() 
        |> Option.map (fun a -> a.SpanId) 
        |> Option.defaultWith ActivitySpanId.CreateRandom

module FeedEvent =
    open System
    open System.Text.Json

    [<CompiledName("From")>]
    let from name payload time version traceId spanId =
        let v = defaultArg version 1s
        let tId = defaultArg traceId (Telemetry.getTraceId())
        let sId = defaultArg spanId (Telemetry.getSpanId())
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

    [<CompiledName("FromObject")>]
    let fromObj name (payload: obj) time version traceId spanId =
        from name (serialize payload) time version traceId spanId

    [<CompiledName("WithDefaults")>]
    let withObjDefaults name (payload: obj) =
        from name (serialize payload) None None None None