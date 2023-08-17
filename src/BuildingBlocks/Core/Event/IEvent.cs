namespace BuildingBlocks.Core.Event;

using MediatR;

using global::MassTransit;

public interface IEvent : INotification
{
    Guid EventId => NewId.NextGuid();
    public DateTime OccurredOn => DateTime.Now;
    public string EventType => GetType().AssemblyQualifiedName;
}
