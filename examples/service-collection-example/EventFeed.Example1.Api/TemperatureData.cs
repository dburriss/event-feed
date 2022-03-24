using Microsoft.Data.SqlClient;
using Dapper;
using EventFeed.Producer.MSSQL;

namespace EventFeed.Example1.Api
{
    public class TempChange
    {
        public float CurrentTemp { get; set; }
        public static TempChange To(float temp) => new TempChange { CurrentTemp = temp };
    }

    public class Temp
    {
        public float Temperature { get; set; }
        public DateOnly Date { get; set; }
    }

    public class TemperatureData : IDisposable
    {
        bool _disposed = false;
        SqlConnection _connection;
        public TemperatureData(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public IEnumerable<Temp> Get()
        {
            return _connection.Query<Temp>("SELECT [temperature], [date] FROM temperatures");
        }

        public void SaveTemp(float temp, DateTime dateTime)
        {
            var sql = @"
IF EXISTS (SELECT id FROM temperatures WITH (UPDLOCK,SERIALIZABLE) WHERE [date] >= @Date AND [date] < DATEADD(DAY, 1, @Date))
BEGIN
   UPDATE temperatures SET [temperature] = @Temp
   WHERE [date] >= @Date AND [date] < DATEADD(DAY, 1, @Date)
END
ELSE
BEGIN
   INSERT INTO temperatures(temperature, date)
   VALUES(@Temp, @Date)
END";
            if (_disposed) { throw new InvalidOperationException("Connection already disposed."); }
            _connection.Open();
            using (var transaction = _connection.BeginTransaction())
            {
                _connection.Execute(sql, new { Date = dateTime, Temp = temp }, transaction);
                var @event = FeedEventModule.WithDefaults("temperature-changed", TempChange.To(temp));
                var events = new NewFeedEvent[1] { @event };
                Events.Save(_connection, transaction, events);
                transaction.Commit();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}
