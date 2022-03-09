module Tests

open Xunit
open EventFeed
open System
open System.Text.Json

[<Fact>]
let ``NewFeedEvent with default has Id and name`` () =
    let name = "test-event"
    let ev = FeedEvent.withDefaults name """{ "test" : true }"""
    Assert.NotEqual(Guid.Empty, ev.EventId)
    Assert.Equal(name, ev.EventName)

[<Fact>]
let ``NewFeedEvent sets activity trace and span`` () =
    let ev = FeedEvent.withDefaults "test-event" """{ "test" : true }"""
    Assert.NotEmpty(ev.TraceId)
    Assert.NotEmpty(ev.SpanId)

[<Fact>]
let ``NewFeedEvent sets date and time`` () =
    let ev = FeedEvent.withDefaults "test-event" """{ "test" : true }"""
    Assert.Equal(DateTimeOffset.UtcNow.Date, ev.CreatedAt.Date)

[<Fact>]
let ``NewFeedEvent with object payload is serialized`` () =
    let o = {| AValue = "value" |}
    let ev = FeedEvent.fromObj "test-event" o None None None None
    let deserialized = JsonSerializer.Deserialize<{| AValue: string |}>(ev.Payload)
    Assert.Equal(o.AValue, deserialized.AValue)
