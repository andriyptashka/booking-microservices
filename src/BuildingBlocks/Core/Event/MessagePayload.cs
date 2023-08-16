using Google.Protobuf;

namespace BuildingBlocks.Core.Event;

public class MessagePayload
{
    public MessagePayload(object? message, IDictionary<string, object?>? headers = null)
    {
        Message = message;
        Headers = headers ?? new Dictionary<string, object?>();
    }

    public object? Message { get; init; }
    public IDictionary<string, object?> Headers { get; init; }
}

public class MessageEnvelope<TMessage> : MessagePayload
    where TMessage : class, IMessage
{
    public MessageEnvelope(TMessage message, IDictionary<string, object?> header) : base(message, header)
    {
        Message = message;
    }

    public new TMessage? Message { get; }
}
