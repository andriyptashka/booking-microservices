namespace BuildingBlocks.Contracts.EventBus.Messages;

using BuildingBlocks.Core.Event;

public record UserCreated(Guid Id, string Name, string PassportNumber) : IIntegrationEvent;
