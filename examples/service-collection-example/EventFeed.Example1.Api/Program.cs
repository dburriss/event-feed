using EventFeed.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEventFeed(_ => new EventFeed.MSSQL.MSSQLEventFeedReader(Environment.GetEnvironmentVariable("EVENTFEED_MSSQL_CONNECTION", EnvironmentVariableTarget.User)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseEventFeed();
app.MapControllers();

app.Run();
