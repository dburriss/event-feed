namespace EventFeed.Producer.MSSQL

open System.Runtime.CompilerServices
open EventFeed.AspNetCore

[<Extension>]
type EventFeedOptionsBuilderExtensions =
    [<Extension>]
    static member inline AddSqlServer(optionsBuilder: EventFeedOptionsBuilder, connectionString: string) =
        optionsBuilder.UseReader(new MSSQLEventFeedReader(connectionString, optionsBuilder.Options.EventsPerPage))