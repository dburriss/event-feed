# Persistence design

This will initially focus around an MS-SQL database as a store. Other stores can be supported in the future. Another consideration is an in-memory store to use for testing.

## Open decisions

- [ ] When should events be moved to cold storage

## Building blocks

- [ ] A sql script for initializing the tables needed
  - [ ] hot event table
  - [ ] cold storage table
  - [ ] metadata table
  - [ ] stored procs**
- [ ] A simple function for storing events within a given transaction

** Stored procs should make usage and using easy but would present a problem from an upgrade point of view. Probably not an issue since anything that would break the stored proc would need to be executed as a script on the DB, which could upgrade the proc too.

### Event table (hot storage)

When persisting an `Event`, it contains the following data with corresponding types:

| Column             | Data Type          | Description                                                                          |
|--------------------|--------------------|--------------------------------------------------------------------------------------|
| Id                 | `bigint`           | Autoincrement numeric internal identifier. Should never be shared.                   |
| EventId            | `uniqueidentifier` | Unique identifier for this specific event                                            |
| EventName          | `VARCHAR(255)`     | The type of event this is eg. `customer-created`                                     |
| EventSchemaVersion | `smallint`         | A version number that increments with changes to the schema                          |
| Payload            | `NVARCHAR(4000)`   | Allows for a JSON payload of 8KB ~ 100 lines of JSON. Events should be small anyway. |
| SpanId             | `CHAR(8)`          | The span identifier. Should be max 8 bytes.                                          |
| TraceId            | `CHAR(16)`         | The trace identifier. Should be max 16 bytes                                         |
| CreatedAt          | `datetimeoffset`   | The date and time the event was created in the application                           |

In addition to storing the above data, the event table needs additional data to help consumers be sure they are receiving events in the order they were created, and they have not missed any events.

### Event archive (cold storage)

The event archive is a copy of the event table with events that are not consumed often. Since data is kept on page access metrics, it can be determined which pages can be moved to cold storage.  
A design question is what should be in cold storage:

- Archive is pages that are cold
- OR Archive is everything but the current page (ie. the page that is not at max page record size)

### Meta table(s)

- oldest page retrieved in last 5 minutes
- oldest page retrieved in last 1 hour
- oldest page retrieved in last 24 hours
- request count per client in last 1 minute
- request count per client in last 30 days

TODO

### Libraries

#### Core

- Events library. Handles creation of events for persistence.
- Serialization of payload
- Populate TraceId and SpanId

| Properties         | .NET Type        | Description                                                                   |
|--------------------|------------------|-------------------------------------------------------------------------------|
| EventId            | `Guid`           | Unique identifier for this specific event                                     |
| EventName          | `string`         | The type of event this is eg. `customer-created`                              |
| EventSchemaVersion | `int16`          | A version number that increments with changes to the schema                   |
| SequenceNumber     | `long`           | A version number that increments with changes to the schema                   |
| Payload            | `string`         | JSON string that contains the contents of the event data                      |
| SpanId             | `string`         | The span identifier. In .NET this is `ActivitySpanId` in `Activity.Current`   |
| TraceId            | `string`         | The trace identifier. In .NET this is `ActivityTraceId` in `Activity.Current` |
| CreatedAt          | `DateTimeOffset` | The date and time the event was created in the application                    |

#### Persistence library

A nuget package with a basic persistence helper. This should consist of a few functions that:

- save an event in the same transaction passed to it (via the connection?)
- retrieve a page of events