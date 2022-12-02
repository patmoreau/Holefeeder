﻿namespace Holefeeder.Domain.Features.Transactions;

public interface ICashflowRepository : IRepository
{
    Task<Cashflow?> FindByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task SaveAsync(Cashflow cashflow, CancellationToken cancellationToken);
}
