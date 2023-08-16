namespace BuildingBlocks.PersistMessageProcessor.Data;

using BuildingBlocks.EFCore;
using Microsoft.EntityFrameworkCore;
using Configurations;
using Core.Model;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;
using IsolationLevel = System.Data.IsolationLevel;

public class PersistMessageDbContext : DbContext, IPersistMessageDbContext
{
    private readonly ILogger<PersistMessageDbContext>? _logger;

    public PersistMessageDbContext(DbContextOptions<PersistMessageDbContext> options,
        ILogger<PersistMessageDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
    }

    public DbSet<PersistMessage> PersistMessages => Set<PersistMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new PersistMessageConfiguration());
        base.OnModelCreating(builder);
        builder.ToSnakeCaseTables();
    }

    public Task ExecuteTransactionalAsync(CancellationToken token = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
            try
            {
                await SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        OnBeforeSaving();

        try
        {
            return await base.SaveChangesAsync(token);
        }

        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(token);

                if (databaseValues == null)
                {
                    _logger.LogError("The record no longer exists in the database, The record has been deleted by another user.");
                    throw;
                }

                entry.OriginalValues.SetValues(databaseValues);
            }

            return await base.SaveChangesAsync(token);
        }
    }

    public void CreatePersistMessageTable()
    {
        if (Database.GetPendingMigrations().Any())
        {
            throw new InvalidOperationException("Cannot create table if there are pending migrations.");
        }

        string createTableSql = @"
            create table if not exists persist_message (
            id uuid not null,
            data_type text,
            data text,
            created timestamp with time zone not null,
            retry_count integer not null,
            message_status text not null default 'InProgress'::text,
            delivery_type text not null default 'Outbox'::text,
            version bigint not null,
            constraint pk_persist_message primary key (id)
            )";

        Database.ExecuteSqlRaw(createTableSql);
    }

    private void OnBeforeSaving()
    {
        try
        {
            foreach (var entry in ChangeTracker.Entries<IVersion>())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.Entity.Version++;
                        break;

                    case EntityState.Deleted:
                        entry.Entity.Version++;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("try for find IVersion", ex);
        }
    }
}
