namespace BuildingBlocks.PersistMessageProcessor;

using System.Linq.Expressions;
using BuildingBlocks.Core.Event;

public interface IPersistMessageProcessor
{
    Task PublishMessageAsync<TMessagePayload>(
        TMessagePayload messagePayload,
        CancellationToken token = default)
        where TMessagePayload : MessagePayload;

    Task<Guid> AddReceivedMessageAsync<TMessagePayload>(
        TMessagePayload messagePayload,
        CancellationToken token = default)
        where TMessagePayload : MessagePayload;

    Task AddInternalMessageAsync<TCommand>(
        TCommand internalCommand,
        CancellationToken token = default)
        where TCommand : class, IInternalCommand;

    Task<IReadOnlyList<PersistMessage>> GetByFilterAsync(
        Expression<Func<PersistMessage, bool>> predicate,
        CancellationToken token = default);

    Task<PersistMessage> ExistMessageAsync(
        Guid messageId,
        CancellationToken token = default);

    Task ProcessInboxAsync(
        Guid messageId,
        CancellationToken token = default);

    Task ProcessAsync(Guid messageId, MessageDeliveryType deliveryType, CancellationToken token = default);

    Task ProcessAllAsync(CancellationToken token = default);
}
