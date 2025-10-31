using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Demo.WeatherForecasts;

namespace Demo.JsonStreaming.AspNetCore.Mvc
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNdjson()
                .AddStreamedJson();

            services.AddSingleton<IWeatherForecaster, WeatherForecaster>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
            defaultFilesOptions.DefaultFileNames.Clear();
            defaultFilesOptions.DefaultFileNames.Add("fetch-streaming.html");

            app.UseCors(policy => policy.WithOrigins("http://localhost:8080", "https://localhost:8081")
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseDefaultFiles(defaultFilesOptions)
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                })
                .Run(async (context) =>
                {
                    await context.Response.WriteAsync("-- Demo.Ndjson.AsyncStreams.AspNetCore.Mvc --");
                });
        }
    }
}
