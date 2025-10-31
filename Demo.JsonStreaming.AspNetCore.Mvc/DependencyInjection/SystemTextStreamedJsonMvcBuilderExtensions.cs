using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class SystemTextStreamedJsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddStreamedJson(this IMvcBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, SystemTextStreamedJsonMvcOptionsSetup>();

            return builder;
        }
    }
}
