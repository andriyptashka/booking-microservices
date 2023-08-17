using MassTransit;

namespace BuildingBlocks.PersistMessageProcessor;

using System.Linq.Expressions;
using System.Text.Json;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PersistMessageProcessor : IPersistMessageProcessor
{
    private readonly ILogger<PersistMessageProcessor> _logger;
    private readonly IMediator _mediator;
    private readonly IPersistMessageDbContext _persistMessageDbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    public PersistMessageProcessor(ILogger<PersistMessageProcessor> logger,
        IMediator mediator,
        IPersistMessageDbContext persistMessageDbContext,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _mediator = mediator;
        _persistMessageDbContext = persistMessageDbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishMessageAsync<T>(T messagePayload, CancellationToken token = default)
        where T : MessagePayload
    {
        await SavePersistMessageAsync(messagePayload, MessageDeliveryType.Outbox, token);
    }

    public Task<Guid> AddReceivedMessageAsync<T>(T messagePayload, CancellationToken token = default)
        where T : MessagePayload
    {
        return SavePersistMessageAsync(messagePayload, MessageDeliveryType.Inbox, token);
    }

    public async Task AddInternalMessageAsync<T>(T internalCommand, CancellationToken token = default)
        where T : class, IInternalCommand
    {
        await SavePersistMessageAsync(new MessagePayload(internalCommand), MessageDeliveryType.Internal, token);
    }

    public async Task<IReadOnlyList<PersistMessage>> GetByFilterAsync(Expression<Func<PersistMessage, bool>> predicate,
        CancellationToken token = default)
    {
        return (await _persistMessageDbContext.PersistMessages.Where(predicate).ToListAsync(token)).AsReadOnly();
    }

    public Task<PersistMessage?> ExistMessageAsync(Guid messageId, CancellationToken token = default)
    {
        return _persistMessageDbContext.PersistMessages.FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                x.DeliveryType == MessageDeliveryType.Inbox &&
                x.MessageStatus == MessageStatus.Processed,
            token);
    }

    public async Task ProcessAsync(
        Guid messageId,
        MessageDeliveryType deliveryType,
        CancellationToken token = default)
    {
        var message = await _persistMessageDbContext.PersistMessages.FirstOrDefaultAsync(
                x => x.Id == messageId && x.DeliveryType == deliveryType, token);

        if (message is null)
        {
            return;
        }

        if (deliveryType == MessageDeliveryType.Internal)
        {
            var isInternalMessageProcessed = await ProcessInternalAsync(message, token);
            if (isInternalMessageProcessed)
            {
                await ChangeMessageStatusAsync(message, token);
            }
            else
            {
                return;
            }
        }

        if (deliveryType == MessageDeliveryType.Outbox)
        {
            var isOutboxMessageProcessed = await ProcessOutboxAsync(message, token);
            if (isOutboxMessageProcessed)
            {
                await ChangeMessageStatusAsync(message, token);
            }
            else
            {
                return;
            }
        }
    }

    public async Task ProcessAllAsync(CancellationToken token = default)
    {
        var messages = await _persistMessageDbContext.PersistMessages
            .Where(x => x.MessageStatus != MessageStatus.Processed)
            .ToListAsync(token);

        foreach (var message in messages)
        {
            await ProcessAsync(message.Id, message.DeliveryType, token);
        }
    }

    public async Task ProcessInboxAsync(Guid messageId, CancellationToken token = default)
    {
        var message = await _persistMessageDbContext.PersistMessages.FirstOrDefaultAsync(
            x => x.Id == messageId &&
                 x.DeliveryType == MessageDeliveryType.Inbox &&
                 x.MessageStatus == MessageStatus.InProgress, token)
            ?? throw new InvalidOperationException($"{nameof(PersistMessage)} with Id:{messageId}, was not found");

        await ChangeMessageStatusAsync(message, token);
    }

    private async Task<bool> ProcessOutboxAsync(PersistMessage message, CancellationToken token)
    {
        var messagePayload = JsonSerializer.Deserialize<MessagePayload>(message.Data);

        if (messagePayload is null || messagePayload.Message is null)
        {
            return false;
        }

        var data = JsonSerializer.Deserialize(messagePayload.Message.ToString() ?? string.Empty,
            TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(message.DataType) ?? typeof(object));

        if (data is not IEvent)
        {
            return false;
        }

        await _publishEndpoint.Publish(data, context =>
        {
            foreach (var header in messagePayload.Headers)
            {
                context.Headers.Set(header.Key, header.Value);
            }
        }, token);

        _logger.LogInformation(
            "Message with id: {MessageId} and delivery type: {DeliveryType} processed from the persistence message store.",
            message.Id,
            message.DeliveryType);

        return true;
    }

    private async Task<bool> ProcessInternalAsync(PersistMessage message, CancellationToken token)
    {
        var messagePayload = JsonSerializer.Deserialize<MessagePayload>(message.Data);

        if (messagePayload is null || messagePayload.Message is null)
        {
            return false;
        }

        var data = JsonSerializer.Deserialize(messagePayload.Message.ToString() ?? string.Empty,
            TypeProvider.GetFirstMatchingTypeFromCurrentDomainAssembly(message.DataType) ?? typeof(object));

        if (data is not IInternalCommand internalCommand)
        {
            return false;
        }

        await _mediator.Send(internalCommand, token);

        _logger.LogInformation(
            "InternalCommand with id: {EventID} and delivery type: {DeliveryType} processed from the persistence message store.",
            message.Id,
            message.DeliveryType);

        return true;
    }

    private async Task<Guid> SavePersistMessageAsync(
        MessagePayload messagePayload,
        MessageDeliveryType deliveryType,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(messagePayload.Message, nameof(messagePayload.Message));

        Guid id;
        if (messagePayload.Message is IEvent message)
        {
            id = message.EventId;
        }
        else
        {
            id = NewId.NextGuid();
        }

        await _persistMessageDbContext.PersistMessages.AddAsync(
            new PersistMessage(
                id,
                messagePayload.Message.GetType().ToString(),
                JsonSerializer.Serialize(messagePayload),
                deliveryType),
            token);

        await _persistMessageDbContext.SaveChangesAsync(token);

        _logger.LogInformation(
            "Message with id: {MessageID} and delivery type: {DeliveryType} saved in persistence message store.",
            id,
            deliveryType.ToString());

        return id;
    }

    private async Task ChangeMessageStatusAsync(PersistMessage message, CancellationToken token)
    {
        message.ChangeStatus(MessageStatus.Processed);

        _persistMessageDbContext.PersistMessages.Update(message);

        await _persistMessageDbContext.SaveChangesAsync(token);
    }
}
