using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.MementoGroup(cfg =>
        cfg.WriteTo.Console(),
        "CorrelationId")
    .CreateLogger();

builder.Logging.AddSerilog(logger);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (ILogger<Program> logger) =>
{
    // start a logging scope so that every log entries have the same CorrelationId in their Properties list
    using var correlationScope = logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", Guid.NewGuid() } });

    // log something that is logged immediately
    logger.LogInformation("Execute {Endpoint}.", "weatherforecast");

    // log something that won't be visible if everything goes well
    logger.LogDebug("There are {SummariesCount} to choose from.", summaries.Length);

    var stopwatch = new Stopwatch();

    stopwatch.Start();
    var forecasts = await GetWeatherForecast(logger, 5);
    stopwatch.Stop();

    if (stopwatch.Elapsed > TimeSpan.FromSeconds(1))
    {
        // log something that won't trigger flushing but has a higher level
        logger.LogWarning("Getting forecast lasted {Duration}, which is longer than the expected!", stopwatch.Elapsed);
    }

    if (forecasts.Length <= 5)
    {
        // log an error to trigger flushing the previously buffered log entries
        logger.LogError("{WeatherForecastCount} was returned whcih is less than the expected!", forecasts.Length);
    }

    return forecasts;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

async Task<WeatherForecast[]> GetWeatherForecast(Microsoft.Extensions.Logging.ILogger logger, int count)
{
    logger.LogDebug("Call weather forecast service with parameter: {Count}.", count);

    await Task.Delay(1100);

    return Enumerable
        .Range(1, count)
        .Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
