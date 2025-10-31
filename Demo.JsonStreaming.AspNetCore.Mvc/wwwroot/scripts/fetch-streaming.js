const FetchStreaming = (function () {

    let abortController;
    let weatherForecastsTable;

    let fetchWeatherForecastsJsonButton, fetchWeatherForecastsJsonStreamButton, fetchWeatherForecastsJsonlStreamButton, postWeatherForecastsJsonlStreamButton, fetchWeatherForecastsNdjsonStreamButton, postWeatherForecastsNdjsonStreamButton, abortButton;

    function initializeUI() {
        fetchWeatherForecastsJsonButton = document.getElementById('fetch-weather-forecasts-json');
        fetchWeatherForecastsJsonButton.addEventListener('click', fetchWeatherForecastsJson);

        fetchWeatherForecastsJsonStreamButton = document.getElementById('fetch-weather-forecasts-json-stream');
        fetchWeatherForecastsJsonStreamButton.addEventListener('click', fetchWeatherForecastsJsonStream);

        fetchWeatherForecastsJsonlStreamButton = document.getElementById('fetch-weather-forecasts-jsonl-stream');
        fetchWeatherForecastsJsonlStreamButton.addEventListener('click', fetchWeatherForecastsJsonlStream);

        postWeatherForecastsJsonlStreamButton = document.getElementById('post-weather-forecasts-jsonl-stream');
        postWeatherForecastsJsonlStreamButton.addEventListener('click', postWeatherForecastsJsonlStream);

        fetchWeatherForecastsNdjsonStreamButton = document.getElementById('fetch-weather-forecasts-ndjson-stream');
        fetchWeatherForecastsNdjsonStreamButton.addEventListener('click', fetchWeatherForecastsNdjsonStream);

        postWeatherForecastsNdjsonStreamButton = document.getElementById('post-weather-forecasts-ndjson-stream');
        postWeatherForecastsNdjsonStreamButton.addEventListener('click', postWeatherForecastsNdjsonStream);

        abortButton = document.getElementById('abort');
        abortButton.addEventListener('click', triggerAbortSignal);

        weatherForecastsTable = document.getElementById('weather-forecasts');
    };

    function fetchWeatherForecastsJson() {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        fetch('api/WeatherForecasts', { signal: abortController.signal })
            .then(function (response) {
                return response.json();
            })
            .then(function (weatherForecasts) {
                weatherForecasts.forEach(appendWeatherForecast);
                switchButtonsState(false);
            });
    };

    function fetchWeatherForecastsJsonStream() {
        abortController = new AbortController();
        const abortSignal = abortController.signal;

        switchButtonsState(true);
        clearWeatherForecasts();

        const oboeInstance = oboe('api/WeatherForecasts/negotiate-stream')
            .node('!.*', function (weatherForecast) {
                appendWeatherForecast(weatherForecast);
            })
            .done(function () {
                switchButtonsState(false);
            });

        abortSignal.onabort = function () {
            oboeInstance.abort();
        };
    }

    function fetchWeatherForecastsJsonlStream() {
        fetchWeatherForecastsJsonlOrNdjonStream('application/jsonl');
    };

    function fetchWeatherForecastsNdjsonStream() {
        fetchWeatherForecastsJsonlOrNdjonStream('application/x-ndjson');
    };

    function fetchWeatherForecastsJsonlOrNdjonStream(accept) {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        fetch('api/WeatherForecasts/negotiate-stream', { headers: { 'Accept': accept }, signal: abortController.signal })
            .then(function (response) {
                const weatherForecasts = response.body
                    .pipeThrough(new TextDecoderStream())
                    .pipeThrough(transformJsonlOrNdjonStream());

                readWeatherForecastsJsonlOrNdjonStream(weatherForecasts.getReader());
            });
    };

    function postWeatherForecastsJsonlStream() {
        postWeatherForecastsJsonlOrNdjonStream('application/jsonl');
    };

    function postWeatherForecastsNdjsonStream() {
        postWeatherForecastsJsonlOrNdjonStream('application/x-ndjson');
    };

    function postWeatherForecastsJsonlOrNdjonStream(contentType) {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        const weatherForecastsStream = WeatherForecaster.getWeatherForecastsStream().pipeThrough(new TextEncoderStream());
        fetch('api/WeatherForecasts/stream', { method: 'POST', headers: { 'Content-Type': contentType }, body: weatherForecastsStream, duplex: 'half', signal: abortController.signal })
            .then(function (response) {
                switchButtonsState(false);
            });
    };

    function triggerAbortSignal() {
        if (abortController) {
            abortController.abort();
            switchButtonsState(false);
        }
    }

    function switchButtonsState(operationInProgress) {
        fetchWeatherForecastsJsonButton.disabled = operationInProgress;
        fetchWeatherForecastsJsonStreamButton.disabled = operationInProgress;
        fetchWeatherForecastsJsonlStreamButton.disabled = operationInProgress;
        postWeatherForecastsJsonlStreamButton.disabled = operationInProgress;
        fetchWeatherForecastsNdjsonStreamButton.disabled = operationInProgress;
        postWeatherForecastsNdjsonStreamButton = operationInProgress;

        abortButton.disabled = !operationInProgress;
    }

    function clearWeatherForecasts() {
        for (let rowIndex = 1; rowIndex  < weatherForecastsTable.rows.length;) {
            weatherForecastsTable.deleteRow(rowIndex );
        }
    };

    function appendWeatherForecast(weatherForecast) {
        let weatherForecastRow = weatherForecastsTable.insertRow(-1);

        weatherForecastRow.insertCell(0).appendChild(document.createTextNode(weatherForecast.dateFormatted));
        weatherForecastRow.insertCell(1).appendChild(document.createTextNode(weatherForecast.temperatureC));
        weatherForecastRow.insertCell(2).appendChild(document.createTextNode(weatherForecast.temperatureF));
        weatherForecastRow.insertCell(3).appendChild(document.createTextNode(weatherForecast.summary));
    };

    function transformJsonlOrNdjonStream() {
        let buffer = '';

        return new TransformStream({
            transform: function(ndjsonChunk, controller) {
                buffer += ndjsonChunk;

                const jsonValues = buffer.split('\n');
                jsonValues.slice(0, -1).forEach(function (jsonValue) { controller.enqueue(JSON.parse(jsonValue)); });

                buffer = jsonValues[jsonValues.length - 1];
            },
            flush: function(controller) {
                if (buffer) {
                    controller.enqueue(JSON.parse(buffer));
                }
            }
        });
    };

    function readWeatherForecastsJsonlOrNdjonStream(weatherForecastsStreamReader) {
        weatherForecastsStreamReader.read()
            .then(function (result) {
                if (!result.done) {
                    appendWeatherForecast(result.value);

                    readWeatherForecastsJsonlOrNdjonStream(weatherForecastsStreamReader);
                } else {
                    switchButtonsState(false);
                }
            });
    };

    return {
        initialize: function () {
            initializeUI();
        }
    };
})();

FetchStreaming.initialize();