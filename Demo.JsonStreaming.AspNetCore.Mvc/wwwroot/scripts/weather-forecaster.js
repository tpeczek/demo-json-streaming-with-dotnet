const WeatherForecaster = (function () {
    const summaries = ['Freezing', 'Bracing', 'Chilly', 'Cool', 'Mild', 'Warm', 'Balmy', 'Hot', 'Sweltering', 'Scorching'];

    function delay(milliseconds) {
        return new Promise((resolve) => setTimeout(resolve, milliseconds));
    };

    async function getRandomWeatherForecast(daysFromToday) {
        await delay(1000);

        const daysFromTodayDate = new Date();
        daysFromTodayDate.setDate(daysFromTodayDate.getDate() + daysFromToday);
        const temperatureC = Math.floor(Math.random() * (75)) - 20;

        return {
            dateFormatted: daysFromTodayDate.toISOString(),
            temperatureC,
            temperatureF: 32 + Math.round(temperatureC / 0.5556),
            summary: summaries[Math.floor(Math.random() * summaries.length)]
        };
    };

    function getRandomWeatherForecastsStream() {
        return new ReadableStream({
            async start(controller) {

                for (let daysFromToday = 1; daysFromToday <= 10; daysFromToday++) {
                    const randomWeatherForecast = await getRandomWeatherForecast(daysFromToday);
                    const randomWeatherForecastJson = JSON.stringify(randomWeatherForecast);
                    controller.enqueue(randomWeatherForecastJson + '\n');
                };
                
                controller.close();
            }
        });
    };

    return {
        getWeatherForecastsStream: function () {
            return getRandomWeatherForecastsStream();
        }
    };
})();