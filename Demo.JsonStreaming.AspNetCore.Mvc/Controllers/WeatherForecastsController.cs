using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ndjson.AsyncStreams.AspNetCore.Mvc;
using Demo.WeatherForecasts;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Demo.JsonStreaming.AspNetCore.Mvc
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherForecastsController : Controller
    {
        private const string APPLICATION_NDJSON_MEDIA_TYPE = "application/x-ndjson";
        private const string APPLICATION_JSONL_MEDIA_TYPE = "application/jsonl";

        private readonly IWeatherForecaster _weatherForecaster;
        private readonly ILogger _logger;

        public WeatherForecastsController(IWeatherForecaster weatherForecaster, ILogger<WeatherForecastsController> logger)
        {
            _weatherForecaster = weatherForecaster;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken cancellationToken)
        {
            List<WeatherForecast> weatherForecasts = new();

            for (int daysFromToday = 1; daysFromToday <= 10; daysFromToday++)
            {
                weatherForecasts.Add(await _weatherForecaster.GetWeatherForecastAsync(daysFromToday, cancellationToken));
            };

            return weatherForecasts;
        }

        [HttpGet("ndjson-stream")]
        // This action always returns NDJSON.
        public NdjsonAsyncEnumerableResult<WeatherForecast> GetNdjsonStream(CancellationToken cancellationToken)
        {
            return new NdjsonAsyncEnumerableResult<WeatherForecast>(StreamWeatherForecastsAsync(cancellationToken), APPLICATION_NDJSON_MEDIA_TYPE);
        }

        [HttpGet("jsonl-stream")]
        // This action always returns JSONL.
        public NdjsonAsyncEnumerableResult<WeatherForecast> GetJsonlStream(CancellationToken cancellationToken)
        {
            return new NdjsonAsyncEnumerableResult<WeatherForecast>(StreamWeatherForecastsAsync(cancellationToken), APPLICATION_JSONL_MEDIA_TYPE);
        }

        [HttpGet("negotiate-stream")]
        // This action returns JSON, JSONL, or NDJSON depending on Accept request header.
        public IAsyncEnumerable<WeatherForecast> NegotiateStream(CancellationToken cancellationToken)
        {
            return StreamWeatherForecastsAsync(cancellationToken);
        }

        [HttpPost("stream")]
        // This action accepts JSONL or NDJSON.
        public async Task<IActionResult> PostStream(IAsyncEnumerable<WeatherForecast> weatherForecasts)
        {
            await foreach (WeatherForecast weatherForecast in weatherForecasts)
            {
                _logger.LogInformation($"{weatherForecast.Summary} ({DateTime.UtcNow})");
            }

            return Ok();
        }

        private async IAsyncEnumerable<WeatherForecast> StreamWeatherForecastsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int daysFromToday = 1; daysFromToday <= 10; daysFromToday++)
            {
                WeatherForecast weatherForecast = await _weatherForecaster.GetWeatherForecastAsync(daysFromToday, cancellationToken);

                yield return weatherForecast;
            };
        }
    }
}
