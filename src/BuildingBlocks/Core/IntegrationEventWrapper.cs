namespace BuildingBlocks.Core;

using BuildingBlocks.Core.Event;

public record IntegrationEventWrapper<T>(T DomainEvent) : IIntegrationEvent where T : IDomainEvent;
