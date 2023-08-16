namespace BuildingBlocks.PersistMessageProcessor;

using Microsoft.EntityFrameworkCore;

public interface IPersistMessageDbContext
{
    DbSet<PersistMessage> PersistMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken token = default);
    Task ExecuteTransactionalAsync(CancellationToken token = default);
}
