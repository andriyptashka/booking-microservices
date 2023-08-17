namespace BuildingBlocks.EFCore;

using BuildingBlocks.Core.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public interface IDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    IReadOnlyList<IDomainEvent> GetDomainEvents();
    Task<int> SaveChangesAsync(CancellationToken token = default);
    Task BeginTransactionAsync(CancellationToken token = default);
    Task CommitTransactionAsync(CancellationToken token = default);
    Task RollbackTransactionAsync(CancellationToken token = default);
    Task ExecuteTransactionalAsync(CancellationToken token = default);
    IExecutionStrategy CreateExecutionStrategy();
}
