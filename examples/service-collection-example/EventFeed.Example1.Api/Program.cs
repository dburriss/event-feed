using EventFeed.AspNetCore;
using EventFeed.Example1.Api;
using EventFeed.MSSQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var config = builder.Configuration.AddJsonFile("appsettings.json", false, true).Build();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEventFeed(_ => new MSSQLEventFeedReader(config.GetConnectionString("Data")));
builder.Services.AddTransient<TemperatureData>(c => new TemperatureData(config.GetConnectionString("Data")));

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
