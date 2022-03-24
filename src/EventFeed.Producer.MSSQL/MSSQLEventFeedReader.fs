namespace EventFeed.MSSQL

open EventFeed.Producer.Abstractions
open Microsoft.Data.SqlClient
open System.Runtime.InteropServices
open EventFeed.Producer.MSSQL
open System.Data.Common
open System.Data

type MSSQLEventFeedReader(connection : SqlConnection, [<Optional; DefaultParameterValue(100)>] eventsPerPage : int) =
    let mutable disposed = false

    let ensureOpen (connection: #DbConnection) =
        if connection.State <> ConnectionState.Open then 
            do connection.Open()
           // System.Threading.Thread.Sleep(10)

    do ensureOpen connection |> ignore

    new(connectionString : string, [<Optional; DefaultParameterValue(100)>] eventsPerPage : int) =
        new MSSQLEventFeedReader(new SqlConnection(connectionString), eventsPerPage)

    interface IEventFeedReader with
        
        member this.EventNumbers() = 
            {
                EventCount = Events.CountEvents(connection)
                EventsPerPage = eventsPerPage
            }

        member this.ReadPage page = 
            Events.GetPage eventsPerPage connection page 
            |> Option.defaultValue Array.empty 
            |> Seq.ofArray

        member this.Dispose() =
            if disposed then ()
            else 
                connection.Dispose()
                disposed <- true