namespace EventFeed.Producer.MSSQL

module Events =

    open System
    open System.Data
    open System.Data.Common
    open Microsoft.Data.SqlClient
    open EventFeed

    let private ensureOpen (connection: #DbConnection) =
        if connection.State <> ConnectionState.Open then 
            do connection.Open()
           // System.Threading.Thread.Sleep(10)

    let private insertSql = """
        INSERT INTO dbo.[__FeedEvents] (EventId, EventName, EventSchemaVersion, Payload, SpanId, CreatedAt, TraceId)
        VALUES (@EventId, @EventName, @EventSchemaVersion, @Payload, @SpanId, @CreatedAt, @TraceId)"""

    let Save (connection: SqlConnection) (transaction : SqlTransaction) (events: NewFeedEvent seq) =

        let insert (connection: SqlConnection) event = 
            let command = new SqlCommand(insertSql, connection)
            command.Transaction <- transaction
            do command.Parameters.AddWithValue("@CreatedAt", event.CreatedAt) |> ignore
            do command.Parameters.AddWithValue("@EventId", event.EventId) |> ignore
            do command.Parameters.AddWithValue("@EventName", event.EventName) |> ignore
            do command.Parameters.AddWithValue("@EventSchemaVersion", event.EventSchemaVersion) |> ignore
            do command.Parameters.AddWithValue("@Payload", event.Payload) |> ignore
            do command.Parameters.AddWithValue("@SpanId", event.SpanId) |> ignore
            do command.Parameters.AddWithValue("@TraceId", event.TraceId) |> ignore

            do command.ExecuteNonQuery() |> ignore
        //ensureOpen connection
        do Seq.iter (insert connection) events
        ()

    let private selectEventSql = """
        SELECT
            [EventId]
            ,[EventName]
            ,[EventSchemaVersion]
            ,[Payload]
            ,[SpanId]
            ,[CreatedAt]
            ,[TraceId]
        FROM [__FeedEvents]
        WHERE [EventId] = @EventId"""

    let GetEventById (connection: SqlConnection) (eventId: Guid) =
        
        let mapRow (reader: SqlDataReader) : NewFeedEvent =
            {
                EventId = reader.GetGuid(0)
                EventName = reader.GetString(1)
                EventSchemaVersion = reader.GetInt16(2)
                Payload = reader.GetString(3)
                SpanId = reader.GetString(4)
                CreatedAt = reader.GetDateTimeOffset(5)
                TraceId = reader.GetString(6)
            }

        let command = new SqlCommand(selectEventSql, connection)
        do command.Parameters.AddWithValue("@EventId", eventId) |> ignore
        let reader = command.ExecuteReader()
        try
            do reader.Read() |> ignore
            mapRow reader
        finally
            reader.Close()

    let private selectPageSql = """
        SELECT 
            [EventId]
            ,[EventName]
            ,[EventSchemaVersion]
            ,[Payload]
            ,[SpanId]
            ,[TraceId]
            ,[CreatedAt]
            ,ROW_NUMBER() OVER(ORDER BY [Id] ASC) AS QueryRowNumber
        FROM [dbo].[__FeedEvents]
        ORDER BY [Id]
        OFFSET @PageSize * (@PageNumber - 1) ROWS
        FETCH NEXT @PageSize ROWS ONLY"""

    let GetPage pageSize (connection: SqlConnection) (page: int) =
        let mapRow (reader: SqlDataReader) : FeedEvent =
            {
                EventId = reader.GetGuid(0)
                EventName = reader.GetString(1)
                EventSchemaVersion = reader.GetInt16(2)
                Payload = reader.GetString(3)
                SpanId = reader.GetString(4)
                TraceId = reader.GetString(5)
                CreatedAt = reader.GetDateTimeOffset(6)
                SequenceNumber = reader.GetInt64(7)
            }

        let command = new SqlCommand(selectPageSql, connection)
        do command.Parameters.AddWithValue("@PageSize", pageSize) |> ignore
        do command.Parameters.AddWithValue("@PageNumber", page) |> ignore
        let reader = command.ExecuteReader()
        try
            if reader.HasRows then
                [|
                    while reader.Read() do
                       mapRow reader 
                |] |> Some
            else None
        finally
            reader.Close()

    let private countEventSql = """SELECT COUNT_BIG([Id]) FROM [__FeedEvents]"""

    let CountEvents (connection: SqlConnection) =
        let command = new SqlCommand(countEventSql, connection)
        command.ExecuteScalar() :?> int64
            