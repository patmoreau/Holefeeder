using System.Data;

using DrifterApps.Seeds.Domain;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.StoreItem;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.Features.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Holefeeder.Application.Context;

public sealed class BudgetingContext : DbContext, IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public BudgetingContext(DbContextOptions<BudgetingContext> options) : base(options) =>
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    public DbSet<Account> Accounts { get; init; } = default!;
    public DbSet<Cashflow> Cashflows { get; init; } = default!;
    public DbSet<Category> Categories { get; init; } = default!;
    public DbSet<StoreItem> StoreItems { get; init; } = default!;
    public DbSet<Transaction> Transactions { get; init; } = default!;
    public DbSet<User> Users { get; init; } = default!;

    public async Task BeginWorkAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction =
            await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task CommitWorkAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);

            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackWorkAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackWorkAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _currentTransaction?.RollbackAsync(cancellationToken)!;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        new CategoryEntityTypeConfiguration().Configure(modelBuilder.Entity<Category>());
        new AccountEntityTypeConfiguration().Configure(modelBuilder.Entity<Account>());
        new CashflowEntityTypeConfiguration().Configure(modelBuilder.Entity<Cashflow>());
        new StoreItemEntityTypeConfiguration().Configure(modelBuilder.Entity<StoreItem>());
        new TransactionEntityTypeConfiguration().Configure(modelBuilder.Entity<Transaction>());
        new UserEntityTypeConfiguration().Configure(modelBuilder.Entity<User>());
        new UserIdentityEntityTypeConfiguration().Configure(modelBuilder.Entity<UserIdentity>());
    }
}
