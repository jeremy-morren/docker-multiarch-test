using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Serilog;
using Shouldly;
using Xunit.Abstractions;

namespace SimpleHttpServer.Tests;

public class HttpServerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SendRequests()
    {
        const int port = 11433;
        var url = $"http://localhost:{port}/{RandomString()}/{RandomString()}?query=123";

        var body = new byte[4096];
        Random.Shared.NextBytes(body);

        var requests = new List<HttpRequest>();
        var handler = new RequestHandler(request =>
            {
                requests.Add(request);
                var json = JsonSerializer.Serialize(request);
                return HttpResponse.Create(HttpStatusCode.OK, json);
            });
        using var server = new HttpServer(IPAddress.Loopback, port, handler, output.CreateTestLogger());
        server.Start();

        using var client = new HttpClient();

        var responses = new List<HttpRequest>();
        for (var i = 0; i < 3; i++)
        {
            // Get request (no body)
            using (var response = await client.GetAsync(url))
                responses.Add(ParseRequest(response));

            // Use ByteArrayContent for Content-Length
            using (var response = await client.PostAsync(url, new ByteArrayContent(body)))
                responses.Add(ParseRequest(response));

            // Use ChunkedContent for chunked transfer encoding
            using (var response = await client.PutAsync(url, new ChunkedContent(body)))
                responses.Add(ParseRequest(response));
        }

        server.Stop();

        requests.Should().HaveCount(9)
            .And.AllSatisfy(r =>
            {
                r.Url.ToString().ShouldBe(url);
                if (r.Method == HttpMethod.Get)
                    r.Body.ShouldBeEmpty();
                else
                    r.Body.ShouldBeEquivalentTo(body);
            })
            .And.BeEquivalentTo(responses);

        Assert.All([HttpMethod.Get, HttpMethod.Post, HttpMethod.Put],
            method => requests.Where(r => r.Method == method).Should().HaveCount(3));
    }

    private static HttpRequest ParseRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream();
        return JsonSerializer.Deserialize<HttpRequest>(stream).ShouldNotBeNull();
    }

    private class RequestHandler(Func<HttpRequest, HttpResponse> handler) : IHttpRequestHandler
    {
        public Task<HttpResponse> Handle(HttpRequest request) => Task.FromResult(handler(request));
    }

    private class ChunkedContent : HttpContent
    {
        private readonly byte[] _content;

        public ChunkedContent(byte[] content)
        {
            _content = content;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var position = 0;
            while (position < _content.Length)
            {
                // Simulate chunk sizes
                var length = position / 2 + 1;
                if (length > _content.Length - position)
                    length = _content.Length - position;
                await stream.WriteAsync(_content.AsMemory(position, length));
                position += length;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    private static string RandomString(int length = 6)
    {
        var chars = Enumerable.Range('a', 'z' - 'a' + 1)
            .Select(c => (char)c)
            .ToArray();
        chars = Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray();
        return new string(chars);
    }
}