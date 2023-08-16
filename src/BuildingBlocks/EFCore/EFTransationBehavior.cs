namespace BuildingBlocks.EFCore;

using BuildingBlocks.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Transactions;
using PersistMessageProcessor;
using Polly;

public class EFTransationBehavior<T, R> : IPipelineBehavior<T, R>
    where T : notnull, IRequest<R>
    where R : notnull
{
    private readonly ILogger<EFTransationBehavior<T, R>> _logger;
    private readonly IDbContext _dbContextBase;
    private readonly IPersistMessageDbContext _persistMessageDbContext;
    private readonly IEventDispatcher _eventDispatcher;

    public EFTransationBehavior(
        ILogger<EFTransationBehavior<T, R>> logger,
        IDbContext dbContextBase,
        IPersistMessageDbContext persistMessageDbContext,
        IEventDispatcher eventDispatcher)
    {
        _logger = logger;
        _dbContextBase = dbContextBase;
        _persistMessageDbContext = persistMessageDbContext;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<R> Handle(T request, RequestHandlerDelegate<R> next, CancellationToken token)
    {
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);

        var response = await next();

        while (true)
        {
            var domainEvents = _dbContextBase.GetDomainEvents();

            if (domainEvents is null || !domainEvents.Any())
            {
                return response;
            }

            await _eventDispatcher.SendAsync(domainEvents.ToArray(), typeof(T), token);
            await _dbContextBase.RetryOnFailure(async () =>
            {
                await _dbContextBase.SaveChangesAsync(token);
            });

            await _persistMessageDbContext.RetryOnFailure(async () =>
            {
                await _persistMessageDbContext.SaveChangesAsync(token);
            });

            scope.Complete();

            return response;
        }
    }
}
