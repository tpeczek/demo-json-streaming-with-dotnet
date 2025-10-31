using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Demo.JsonStreaming.AspNetCore.Mvc.Formatters;


namespace Microsoft.Extensions.DependencyInjection
{
    internal class SystemTextStreamedJsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly IOptions<JsonOptions>? _jsonOptions;
        private readonly ILogger<SystemTextJsonInputFormatter> _inputFormatterLogger;

        public SystemTextStreamedJsonMvcOptionsSetup(IOptions<JsonOptions>? jsonOptions, ILogger<SystemTextJsonInputFormatter> inputFormatterLogger)
        {
            _jsonOptions = jsonOptions;
            _inputFormatterLogger = inputFormatterLogger ?? throw new ArgumentNullException(nameof(inputFormatterLogger));
        }

        public void Configure(MvcOptions options)
        {
            options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
            options.InputFormatters.Add(new SystemTextStreamedJsonInputFormatter(_jsonOptions?.Value, _inputFormatterLogger));
        }
    }
}
