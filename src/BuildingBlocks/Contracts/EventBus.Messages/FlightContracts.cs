namespace BuildingBlocks.Contracts.EventBus.Messages;

using BuildingBlocks.Core.Event;

public record FlightCreated(Guid Id) : IIntegrationEvent;
public record FlightUpdated(Guid Id) : IIntegrationEvent;
public record FlightDeleted(Guid Id) : IIntegrationEvent;
public record AircraftCreated(Guid Id) : IIntegrationEvent;
public record AirportCreated(Guid Id) : IIntegrationEvent;
public record SeatCreated(Guid Id) : IIntegrationEvent;
public record SeatReserved(Guid Id) : IIntegrationEvent;
