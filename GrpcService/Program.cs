using GrpcService;
using System.IO.Pipelines;
using System.Net;

var app = WebApplication.Create();
app.MapPost("/unary", HandleUnaryCallAsync);
app.MapPost("/serverstream", HandleServerStreamCallAsync);
app.MapPost("/clientstream", HandleClientStreamCallAsync);
await app.StartAsync();

await UnaryCallAsync();
await ServerStreamCallAsync();
await ClientStreamCallAsync();
await BidirectionalStreamCallAsync();

Console.ReadLine();

static async Task HandleUnaryCallAsync(HttpContext httpContext)
{
    var reader = httpContext.Request.BodyReader;
    var write = httpContext.Response.BodyWriter;
    await reader.ReadAndProcessAsync(HelloRequest.Parser, async hello =>
    {
        var reply = new HelloReply { Message = $"Hello, {hello.Names}!" };
        await write.WriteMessageAsync(reply);
    });
}

static async Task UnaryCallAsync()
{
    using (var httpClient = new HttpClient())
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/unary")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new MessageContent(new HelloRequest { Names = "foobar" })
        };
        var reply = await httpClient.SendAsync(request);
        await PipeReader.Create(await reply.Content.ReadAsStreamAsync()).ReadAndProcessAsync(HelloReply.Parser, reply =>
        {
            Console.WriteLine(reply.Message);
            return Task.CompletedTask;
        });
    }
}

static async Task HandleServerStreamCallAsync(HttpContext httpContext)
{
    var reader = httpContext.Request.BodyReader;
    var write = httpContext.Response.BodyWriter;
    await reader.ReadAndProcessAsync(HelloRequest.Parser, async hello =>
    {
        var names = hello.Names.Split(',');
        foreach (var name in names)
        {
            var reply = new HelloReply { Message = $"Hello, {name}!" };
            await write.WriteMessageAsync(reply);
            await Task.Delay(1000);
        }
    });
}

static async Task ServerStreamCallAsync()
{
    using (var httpClient = new HttpClient())
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/serverstream")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new MessageContent(new HelloRequest { Names = "foo,bar,baz,qux" })
        };
        var reply = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        await PipeReader.Create(await reply.Content.ReadAsStreamAsync()).ReadAndProcessAsync(HelloReply.Parser, reply =>
        {
            Console.WriteLine($"[{DateTimeOffset.Now}]{reply.Message}");
            return Task.CompletedTask;
        });
    }
}

static async Task HandleClientStreamCallAsync(HttpContext httpContext)
{
    var reader = httpContext.Request.BodyReader;
    var write = httpContext.Response.BodyWriter;
    await reader.ReadAndProcessAsync(HelloRequest.Parser, async hello =>
    {
        var names = hello.Names.Split(',');
        foreach (var name in names)
        {
            Console.WriteLine($"[{DateTimeOffset.Now}]Hello, {name}!");
            await Task.Delay(1000);
        }
    });
}

static async Task ClientStreamCallAsync()
{
    using (var httpClient = new HttpClient())
    {
        var writer = new ClientStreamWriter<HelloRequest>();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/clientstream")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ClientStreamContent<HelloRequest>(writer)
        };
        _ =  httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        foreach (var name in new string[] {"foo","bar","baz","qux" })
        {
            await writer.WriteAsync(new HelloRequest { Names = name});
            await Task.Delay(1000);
        }
        writer.Complete();
    }
}

static async Task BidirectionalStreamCallAsync()
{
    using (var httpClient = new HttpClient())
    {
        var writer = new ClientStreamWriter<HelloRequest>();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/unary")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ClientStreamContent<HelloRequest>(writer)
        };
        var task = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        _ = Task.Run(async () =>
        {
            var response = await task;
            await PipeReader.Create(await response.Content.ReadAsStreamAsync()).ReadAndProcessAsync(HelloReply.Parser, reply =>
            {
                Console.WriteLine($"[{DateTimeOffset.Now}]{reply.Message}");
                return Task.CompletedTask;
            });
        });

        foreach (var name in new string[] { "foo", "bar", "baz", "qux" })
        {
            await writer.WriteAsync(new HelloRequest { Names = name });
            await Task.Delay(1000);
        }
        writer.Complete();
    }
}