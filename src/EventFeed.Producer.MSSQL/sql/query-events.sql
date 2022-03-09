-- what about arhcive count? Should be checked in same query.
DECLARE @PageNumber INT = 1;
DECLARE @PageSize   INT = 10;
SELECT [Id]
      ,[EventId]
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
  FETCH NEXT @PageSize ROWS ONLY;
  