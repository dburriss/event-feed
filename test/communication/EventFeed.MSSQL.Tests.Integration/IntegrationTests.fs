namespace EventFeed.MSSQL

module IntegrationTests =

    open System
    open Xunit
    open Microsoft.Data.SqlClient
    open System.Data
    open System.Data.Common
    open EventFeed
    open EventFeed.Producer.MSSQL

    let connectionString() = 
        let c = Environment.GetEnvironmentVariable("EVENTFEED_MSSQL_CONNECTION", EnvironmentVariableTarget.User)
        c

    let private ensureOpen (connection: #DbConnection) =
        if connection.State <> ConnectionState.Open then 
            connection.Open()
            //Threading.Thread.Sleep(100)

    let numberOfEvents connString =
        use countConn = new SqlConnection(connString)
        ensureOpen countConn
        // do ensureOpen conn
        let command = new SqlCommand("SELECT Count(Id) FROM __FeedEvents", countConn)
        command.ExecuteScalar() :?> int

    
    [<Fact>]
    let ``Events in rollback transaction not saved`` () =

        let connString = connectionString()
        use insertConn = new SqlConnection(connString)
        ensureOpen insertConn
        try
            use transaction = insertConn.BeginTransaction()
            let oldRowCount = numberOfEvents connString
            let newEvent : NewFeedEvent = { 
                CreatedAt = DateTimeOffset.UtcNow
                EventId = Guid.NewGuid()
                EventName = "test-click-event"
                EventSchemaVersion = 1s
                Payload = """{ "clicked": true }"""
                SpanId = "abc1230xyz"
                TraceId = "abctraceid1234567890"
            }
            Events.Save insertConn transaction [newEvent]
            do transaction.Rollback()
            let rowCount = numberOfEvents connString
            Assert.Equal(oldRowCount, rowCount)
        finally
            insertConn.Dispose()
    
    
    [<Fact>]
    let ``Events in committed transactionnot are saved`` () =

        let connString = connectionString()
        use insertConn = new SqlConnection(connString)
        ensureOpen insertConn
        try
            use transaction = insertConn.BeginTransaction()
            let oldRowCount = numberOfEvents connString
            let newEvent : NewFeedEvent = { 
                CreatedAt = DateTimeOffset.UtcNow
                EventId = Guid.NewGuid()
                EventName = "test-click-event"
                EventSchemaVersion = 1s
                Payload = """{ "clicked": true }"""
                SpanId = "abc1230xyz"
                TraceId = "abctraceid1234567890"
            }
            let events = [newEvent]
            Events.Save insertConn transaction [newEvent]
            do transaction.Commit()
            let newRowCount = numberOfEvents connString
            Assert.Equal(oldRowCount + events.Length, newRowCount)
        finally
            insertConn.Dispose()

    
    [<Fact>]
    let ``NewEvent persisted to row`` () =

        let connString = connectionString()
        use insertConn = new SqlConnection(connString)
        ensureOpen insertConn
        try
            use transaction = insertConn.BeginTransaction()
            let newEvent : NewFeedEvent = { 
                CreatedAt = DateTimeOffset.UtcNow
                EventId = Guid.NewGuid()
                EventName = "test-click-event"
                EventSchemaVersion = 1s
                Payload = """{ "clicked": true }"""
                SpanId = "abc1230xyz"
                TraceId = "abctraceid1234567890"
            }
            Events.Save insertConn transaction [newEvent]
            do transaction.Commit()
            let ev = Events.GetEventById insertConn newEvent.EventId

            Assert.Equal(newEvent.CreatedAt, ev.CreatedAt)
            Assert.Equal(newEvent.EventId, ev.EventId)
            Assert.Equal(newEvent.EventName, ev.EventName)
            Assert.Equal(newEvent.EventSchemaVersion, ev.EventSchemaVersion)
            Assert.Equal(newEvent.Payload, ev.Payload)
            Assert.Equal(newEvent.SpanId, ev.SpanId)
            Assert.Equal(newEvent.TraceId, ev.TraceId)
        finally
            insertConn.Dispose()


    [<Fact>]
    // this test assumes data exists
    let ``Paging returns consecutive numbers`` () =

        let connString = connectionString()
        use conn = new SqlConnection(connString)
        let pageSize = 5

        let rec fetchEvents events page =
            match (Events.GetPage pageSize conn page) with
            | Some evs -> fetchEvents (Array.append events evs) (page + 1)
            | None -> events

        ensureOpen conn
        try
            let events = fetchEvents Array.empty 1
            let mutable i = 0L
        
            for e in events do
                i <- i + 1L
                Assert.Equal(i, e.SequenceNumber)
        
            Assert.NotEmpty events
        finally
            conn.Dispose()