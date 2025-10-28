const FetchStreaming = (function () {

    let abortController;
    let weatherForecastsTable;

    let fetchWeatherForecastsJsonButton, fetchWeatherForecastsJsonStreamButton, fetchWeatherForecastsJsonlStreamButton, fetchWeatherForecastsNdjsonStreamButton, postWeatherForecastsNdjsonStreamButton, abortButton;

    function initializeUI() {
        fetchWeatherForecastsJsonButton = document.getElementById('fetch-weather-forecasts-json');
        fetchWeatherForecastsJsonButton.addEventListener('click', fetchWeatherForecastsJson);

        fetchWeatherForecastsJsonStreamButton = document.getElementById('fetch-weather-forecasts-json-stream');
        fetchWeatherForecastsJsonStreamButton.addEventListener('click', fetchWeatherForecastsJsonStream);

        fetchWeatherForecastsJsonlStreamButton = document.getElementById('fetch-weather-forecasts-jsonl-stream');
        fetchWeatherForecastsJsonlStreamButton.addEventListener('click', fetchWeatherForecastsJsonlStream);

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

    function fetchWeatherForecastsNdjsonStream() {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        fetch('api/WeatherForecasts/negotiate-stream', { headers: { 'Accept': 'application/x-ndjson' }, signal: abortController.signal })
            .then(function (response) {
                const weatherForecasts = response.body
                    .pipeThrough(new TextDecoderStream())
                    .pipeThrough(transformNdjsonStream());

                readWeatherForecastsNdjsonStream(weatherForecasts.getReader());
            });
    };

    function fetchWeatherForecastsJsonlStream() {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        fetch('api/WeatherForecasts/negotiate-stream', { headers: { 'Accept': 'application/jsonl' }, signal: abortController.signal })
            .then(function (response) {
                const weatherForecasts = response.body
                    .pipeThrough(new TextDecoderStream())
                    .pipeThrough(transformNdjsonStream());

                readWeatherForecastsNdjsonStream(weatherForecasts.getReader());
            });
    };

    function postWeatherForecastsNdjsonStream() {
        abortController = new AbortController();

        switchButtonsState(true);
        clearWeatherForecasts();

        const weatherForecastsStream = WeatherForecaster.getWeatherForecastsStream().pipeThrough(new TextEncoderStream());
        fetch('api/WeatherForecasts/stream', { method: 'POST', headers: { 'Content-Type': 'application/x-ndjson' }, body: weatherForecastsStream, duplex: 'half', signal: abortController.signal })
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

    function transformNdjsonStream() {
        let ndjsonBuffer = '';

        return new TransformStream({
            transform: function(ndjsonChunk, controller) {
                ndjsonBuffer += ndjsonChunk;

                const jsonValues = ndjsonBuffer.split('\n');
                jsonValues.slice(0, -1).forEach(function (jsonValue) { controller.enqueue(JSON.parse(jsonValue)); });

                ndjsonBuffer = jsonValues[jsonValues.length - 1];
            },
            flush: function(controller) {
                if (ndjsonBuffer) {
                    controller.enqueue(JSON.parse(ndjsonBuffer));
                }
            }
        });
    };

    function readWeatherForecastsNdjsonStream(weatherForecastsStreamReader) {
        weatherForecastsStreamReader.read()
            .then(function (result) {
                if (!result.done) {
                    appendWeatherForecast(result.value);

                    readWeatherForecastsNdjsonStream(weatherForecastsStreamReader);
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