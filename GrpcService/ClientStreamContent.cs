using Google.Protobuf;
using System.Net;

namespace GrpcService
{
public class ClientStreamContent<TMessage> : HttpContent where TMessage:IMessage<TMessage>
{
    private readonly ClientStreamWriter<TMessage> _writer;
    public ClientStreamContent(ClientStreamWriter<TMessage> writer)=> _writer = writer;
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => _writer.SetOutputStream(stream).WaitAsync();
    protected override bool TryComputeLength(out long length) => (length = -1) != -1;
}
}
