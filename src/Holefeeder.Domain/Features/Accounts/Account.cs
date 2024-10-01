using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.Features.Users;
using Holefeeder.Domain.ValueObjects;

namespace Holefeeder.Domain.Features.Accounts;

public sealed partial record Account : IAggregateRoot
{
    private Account(AccountId id, AccountType type, string name, DateOnly openDate, UserId userId)
    {
        Id = id;
        Type = type;
        Name = name;
        OpenDate = openDate;
        UserId = userId;
    }

    public AccountId Id { get; }

    public AccountType Type { get; private init; }

    public string Name { get; private init; }

    public bool Favorite { get; private init; }

    public Money OpenBalance { get; private init; }

    public DateOnly OpenDate { get; private init; }

    public string Description { get; private init; } = string.Empty;

    public bool Inactive { get; private init; }

    public UserId UserId { get; }

    public IReadOnlyCollection<Cashflow> Cashflows { get; init; } = new List<Cashflow>();
    public IReadOnlyCollection<Transaction> Transactions { get; init; } = new List<Transaction>();

    public decimal CalculateBalance() =>
        OpenBalance + Transactions.Sum(t => t.Amount * t.Category!.Type.Multiplier * Type.Multiplier);

    public DateOnly CalculateLastTransactionDate() => Transactions.Count > 0 ? Transactions.Max(t => t.Date) : OpenDate;
}

public sealed record AccountId : StronglyTypedId<AccountId>;
