using Google.Protobuf;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace GrpcService
{
public static class ReadWriteExtensions
{
    public static ValueTask<FlushResult> WriteMessageAsync(this PipeWriter writer, IMessage message)
    {
        var length = message.CalculateSize();
        var span = writer.GetSpan(4+length);
        BitConverter.GetBytes(length).CopyTo(span);
        message.WriteTo(span.Slice(4, length));
        writer.Advance(4 + length);
        return writer.FlushAsync();
    }

    public static async Task ReadAndProcessAsync<TMessage>(this PipeReader reader, MessageParser<TMessage> parser, Func<TMessage, Task> handler) where TMessage:IMessage<TMessage>
    { 
        while(true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;
            while (TryReadMessage(ref buffer, out var message))
            {
                await handler(message!);
            }
            reader.AdvanceTo(buffer.Start, buffer.End);
            if(result.IsCompleted)
            {
                break;
            }
        }


        bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out TMessage? message) 
        {
            if(buffer.Length < 4)
            {
                message = default;
                return false;
            }

            Span<byte> lengthBytes = stackalloc byte[4];
            buffer.Slice(0,4).CopyTo(lengthBytes);
            var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
            if (buffer.Length < length + 4)
            {
                message = default;
                return false;
            }

            message = parser.ParseFrom(buffer.Slice(4, length));
            buffer = buffer.Slice(length + 4);
            return true;
        }
    }
}
}
