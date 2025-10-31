using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace Demo.JsonStreaming.AspNetCore.Mvc.Formatters
{
    internal class SystemTextStreamedJsonInputFormatter : SystemTextJsonInputFormatter
    {
        private static readonly Type _jsonSerializerType = typeof(JsonSerializer);
        private static readonly Type _asyncEnumerableType = typeof(IAsyncEnumerable<>);
        private static readonly string _deserializeAsyncEnumerableMethodName = nameof(JsonSerializer.DeserializeAsyncEnumerable);
        private static readonly Type[] _deserializeAsyncEnumerableMethodParametersTypes = [typeof(Stream), typeof(JsonSerializerOptions), typeof(CancellationToken)];

        private readonly ConcurrentDictionary<Type, MethodInfo> _deserializeAsyncEnumerableMethods = new();

        public SystemTextStreamedJsonInputFormatter(JsonOptions options, ILogger<SystemTextJsonInputFormatter> logger)
            : base(options, logger)
        { }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (context.ModelType.GetGenericTypeDefinition() == _asyncEnumerableType)
            {
                MethodInfo deserializeAsyncEnumerableMethod = GetDeserializeAsyncEnumerableMethod(context.ModelType);

                return Task.FromResult(InputFormatterResult.Success(deserializeAsyncEnumerableMethod.Invoke(null, [context.HttpContext.Request.Body, base.SerializerOptions, context.HttpContext.RequestAborted])));
            }

            return base.ReadRequestBodyAsync(context);
        }

        private MethodInfo GetDeserializeAsyncEnumerableMethod(Type modelType)
        {
            MethodInfo deserializeAsyncEnumerableMethod;
            if (!_deserializeAsyncEnumerableMethods.TryGetValue(modelType, out deserializeAsyncEnumerableMethod))
            {
                deserializeAsyncEnumerableMethod = _jsonSerializerType
                    .GetMethod(_deserializeAsyncEnumerableMethodName, _deserializeAsyncEnumerableMethodParametersTypes)
                    .MakeGenericMethod(modelType.GetGenericArguments()[0]);

                _deserializeAsyncEnumerableMethods.TryAdd(modelType, deserializeAsyncEnumerableMethod);
            }

            return deserializeAsyncEnumerableMethod;
        }
    }
}
