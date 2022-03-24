CREATE TABLE [dbo].[__FeedEvents](
	[Id] [bigint] IDENTITY(1,1) PRIMARY KEY,
	[CreatedAt] [datetimeoffset] NOT NULL,
	[EventId] [uniqueidentifier] NOT NULL,
	[EventName] [varchar](255) NOT NULL,
	[EventSchemaVersion] [smallint] NOT NULL,
	[Payload] [nvarchar](4000) NOT NULL,
	[SpanId] [varchar](50) NOT NULL,
	[TraceId] [varchar](100) NOT NULL,
)