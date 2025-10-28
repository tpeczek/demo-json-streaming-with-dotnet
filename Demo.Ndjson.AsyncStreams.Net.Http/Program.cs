using System;
using System.IO;
using System.Text;
using System.Buffers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ndjson.AsyncStreams.Net.Http;
using Demo.WeatherForecasts;

namespace Demo.Ndjson.AsyncStreams.Net.Http
{
    class Program
    {
        // Demo.Ndjson.AsyncStreams.AspNetCore.Mvc
        private const string BASE_URL = "https://localhost:5001";

        // Demo.JsonStreaming.AspNetCore.MinimalApi
        // private const string BASE_URL = "https://localhost:7188";

        private static readonly IWeatherForecaster _weatherForecaster = new WeatherForecaster();

        static async Task Main(string[] args)
        {
            using CancellationTokenSource cancellationTokenSource = new();

            void consoleCancelEventHandler(object sender, ConsoleCancelEventArgs eventArgs)
            {
                Console.WriteLine("Attempting to cancel  . . .");

                cancellationTokenSource.Cancel();
                eventArgs.Cancel = true;
            };
            Console.CancelKeyPress += consoleCancelEventHandler;

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                await ConsumeJsonlOrNdjsonStreamAsync("ndjson-stream", cancellationToken).ConfigureAwait(false);
                
                await ConsumeJsonlOrNdjsonStreamAsync("jsonl-stream", cancellationToken).ConfigureAwait(false);

                await NegotiateRawJsonStreamAsync(cancellationToken).ConfigureAwait(false);

                await NegotiateJsonStreamAsync(cancellationToken).ConfigureAwait(false);

                await NegotiateJsonlOrNdjsonStreamAsync("application/x-ndjson", cancellationToken).ConfigureAwait(false);

                await NegotiateJsonlOrNdjsonStreamAsync("application/jsonl", cancellationToken).ConfigureAwait(false);

                await StreamJsonStreamAsync(cancellationToken).ConfigureAwait(false);

                await StreamNdjsonAsync(cancellationToken).ConfigureAwait(false);

                Console.CancelKeyPress -= consoleCancelEventHandler;

                Console.WriteLine("Press any key to exit . . .");
                Console.ReadKey(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Successfully cancelled.");
            }
        }

        private static async Task ConsumeJsonlOrNdjsonStreamAsync(string streamEndpoint, CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(ConsumeJsonlOrNdjsonStreamAsync)}({streamEndpoint}) --");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Receving weather forecasts . . .");

            using HttpClient httpClient = new();

            using HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/api/WeatherForecasts/{streamEndpoint}", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await foreach (WeatherForecast weatherForecast in response.Content!.ReadFromNdjsonAsync<WeatherForecast>(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] {weatherForecast.Summary}");
            }

            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Weather forecasts has been received.");
            Console.WriteLine();
        }

        private static async Task NegotiateRawJsonStreamAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(NegotiateRawJsonStreamAsync)} --");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Receving weather forecasts . . .");

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            using HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/api/WeatherForecasts/negotiate-stream", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            while (true)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(128);
                int bytesRead = await responseStream.ReadAsync(buffer, cancellationToken);

                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] ({bytesRead}/{buffer.Length}) {Encoding.UTF8.GetString(buffer)}");

                ArrayPool<byte>.Shared.Return(buffer);

                if (bytesRead == 0)
                {
                    break;
                }
            }

            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Weather forecasts has been received.");
            Console.WriteLine();
        }

        private static async Task NegotiateJsonStreamAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(NegotiateJsonStreamAsync)} --");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Receving weather forecasts . . .");

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            using HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/api/WeatherForecasts/negotiate-stream", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            await foreach (WeatherForecast weatherForecast in JsonSerializer.DeserializeAsyncEnumerable<WeatherForecast>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 128 }, cancellationToken))
            {
                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] {weatherForecast.Summary}");
            }

            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Weather forecasts has been received.");
            Console.WriteLine();
        }

        private static async Task NegotiateJsonlOrNdjsonStreamAsync(string accept, CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(NegotiateJsonlOrNdjsonStreamAsync)}({accept}) --");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Receving weather forecasts . . .");

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Accept", accept);

            using HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/api/WeatherForecasts/negotiate-stream", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await foreach (WeatherForecast weatherForecast in response.Content!.ReadFromNdjsonAsync<WeatherForecast>(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] {weatherForecast.Summary}");
            }

            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Weather forecasts has been received.");
            Console.WriteLine();
        }

        private static async Task StreamNdjsonAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(StreamNdjsonAsync)} --");
            Console.WriteLine("Sending weather forecasts . . .");

            using HttpClient httpClient = new();

            using HttpResponseMessage response = await httpClient.PostAsNdjsonAsync($"{BASE_URL}/api/WeatherForecasts/stream", StreamWeatherForecastsAsync(), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Weather forecasts has been send.");
            Console.WriteLine();
        }

        private static async Task StreamJsonStreamAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"-- {nameof(StreamJsonStreamAsync)} --");
            Console.WriteLine("Sending weather forecasts . . .");

            using HttpClient httpClient = new();

            JsonAsyncEnumerableContent<WeatherForecast> jsonAsyncEnumerableContent = new JsonAsyncEnumerableContent<WeatherForecast>(StreamWeatherForecastsAsync());
            using HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/WeatherForecasts/stream", jsonAsyncEnumerableContent, cancellationToken);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Weather forecasts has been send.");
            Console.WriteLine();
        }

        private static async IAsyncEnumerable<WeatherForecast> StreamWeatherForecastsAsync()
        {
            for (int daysFromToday = 1; daysFromToday <= 10; daysFromToday++)
            {
                WeatherForecast weatherForecast = await _weatherForecaster.GetWeatherForecastAsync(daysFromToday).ConfigureAwait(false);

                yield return weatherForecast;
            };
        }
    }
}
