using Google.Protobuf;
using GrpcService;
using System.IO.Pipelines;
using System.Net;

public class MessageContent : HttpContent
{
    private readonly IMessage _message;
    public MessageContent(IMessage message) => _message = message;
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    =>await PipeWriter.Create(stream).WriteMessageAsync(_message);
    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}
