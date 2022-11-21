using Google.Protobuf;
using System.IO.Pipelines;

namespace GrpcService
{
public class ClientStreamWriter<TMessage> where TMessage: IMessage<TMessage>
{
    private readonly TaskCompletionSource<Stream> _streamSetSource = new();
    private readonly TaskCompletionSource _streamEndSuource = new();

    public ClientStreamWriter<TMessage> SetOutputStream(Stream outputStream)
    {
        _streamSetSource.SetResult(outputStream);
        return this;
    }

    public async Task WriteAsync(TMessage message)
    {
        var stream = await _streamSetSource.Task;
        await PipeWriter.Create(stream).WriteMessageAsync(message);
    }

    public void Complete()=> _streamEndSuource.SetResult();
    public Task WaitAsync() => _streamEndSuource.Task;
}
}
