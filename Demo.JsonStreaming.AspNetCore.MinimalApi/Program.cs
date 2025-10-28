using Demo.WeatherForecasts;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IWeatherForecaster, WeatherForecaster>();

var app = builder.Build();

app.MapGet("/", () => "-- Demo.Ndjson.AsyncStreams.AspNetCore.MinimalApi --");

// This endpoint returns JSON.
app.MapGet("/api/WeatherForecasts", async (IWeatherForecaster weatherForecaster, CancellationToken cancellationToken) =>
{
    List<WeatherForecast> weatherForecasts = new();

    for (int daysFromToday = 1; daysFromToday <= 10; daysFromToday++)
    {
        weatherForecasts.Add(await weatherForecaster.GetWeatherForecastAsync(daysFromToday, cancellationToken));
    };

    return weatherForecasts;
});

// This endpoint returns streamed JSON.
app.MapGet("/api/WeatherForecasts/negotiate-stream", (IWeatherForecaster weatherForecaster, CancellationToken cancellationToken) => StreamWeatherForecastsAsync(weatherForecaster, cancellationToken));

// This endpoint streams NDJSON.
app.MapGet("/api/WeatherForecasts/ndjson-stream", (IWeatherForecaster weatherForecaster, CancellationToken cancellationToken) => Results.Extensions.Ndjson(StreamWeatherForecastsAsync(weatherForecaster, cancellationToken)));

// This endpoint streams JSONL.
app.MapGet("/api/WeatherForecasts/jsonl-stream", (IWeatherForecaster weatherForecaster, CancellationToken cancellationToken) => Results.Extensions.Jsonl(StreamWeatherForecastsAsync(weatherForecaster, cancellationToken)));

app.Run();

static async IAsyncEnumerable<WeatherForecast> StreamWeatherForecastsAsync(IWeatherForecaster weatherForecaster, [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (int daysFromToday = 1; daysFromToday <= 10; daysFromToday++)
    {
        WeatherForecast weatherForecast = await weatherForecaster.GetWeatherForecastAsync(daysFromToday, cancellationToken);

        yield return weatherForecast;
    };
}
