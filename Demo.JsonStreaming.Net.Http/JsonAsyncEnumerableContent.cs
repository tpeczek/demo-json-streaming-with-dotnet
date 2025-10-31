using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Demo.JsonStreaming.Net.Http
{
    internal class JsonAsyncEnumerableContent<T> : HttpContent
    {
        private class JsonAsyncEnumerableStream : Stream
        {
            private readonly Stream _stream;

            public JsonAsyncEnumerableStream(Stream stream)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public override bool CanRead { get { return _stream.CanRead; } }

            public override bool CanSeek { get { return _stream.CanSeek; } }

            public override bool CanWrite { get { return _stream.CanWrite; } }

            public override long Length { get { return _stream.Length; } }

            public override long Position
            {
                get { return _stream.Position; }

                set { _stream.Position = value; }
            }

            public override void Flush() => _stream.Flush();

            public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

            public override void SetLength(long value) => _stream.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
                _stream.Flush();
            }
        }

        private static readonly JsonSerializerOptions _defaultJsonSerializerOptions = new(JsonSerializerDefaults.Web);
        
        private readonly IAsyncEnumerable<T> _values;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        
        public JsonAsyncEnumerableContent(IAsyncEnumerable<T> values, JsonSerializerOptions options = null)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _jsonSerializerOptions = options ?? _defaultJsonSerializerOptions;

            Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = Encoding.UTF8.WebName
            };
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            JsonAsyncEnumerableStream jsonAsyncEnumerableStream = new(stream);

            return JsonSerializer.SerializeAsync(jsonAsyncEnumerableStream, _values, _jsonSerializerOptions, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            return false;
        }
    }
}
