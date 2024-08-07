using DrifterApps.Seeds.Testing;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Tests.Common.Extensions;

namespace Holefeeder.Tests.Common.Builders.Transactions;

internal class TransactionBuilder : FakerBuilder<Transaction>
{
    protected override Faker<Transaction> FakerRules { get; } = new Faker<Transaction>()
        .RuleFor(x => x.Id, faker => faker.RandomGuid())
        .RuleFor(x => x.Date, faker => faker.Date.RecentDateOnly())
        .RuleFor(x => x.Amount, faker => faker.Finance.Amount(min: 1))
        .RuleFor(x => x.Description, faker => faker.Lorem.Sentence())
        .RuleFor(x => x.AccountId, faker => faker.RandomGuid())
        .RuleFor(x => x.Account, _ => default)
        .RuleFor(x => x.CategoryId, faker => faker.RandomGuid())
        .RuleFor(x => x.Category, _ => default)
        .RuleFor(x => x.CashflowId, _ => default)
        .RuleFor(x => x.CashflowDate, _ => default)
        .RuleFor(x => x.Cashflow, _ => default)
        .RuleFor(x => x.UserId, faker => faker.RandomGuid())
        .RuleFor(x => x.Tags, faker => faker.Lorem.Words(faker.Random.Int(1, 10)).Distinct().ToArray());

    public static TransactionBuilder GivenATransaction() => new();

    public TransactionBuilder WithNoId()
    {
        FakerRules.RuleFor(f => f.Id, _ => Guid.Empty);
        return this;
    }

    public TransactionBuilder OfAmount(decimal amount)
    {
        FakerRules.RuleFor(f => f.Amount, amount);
        return this;
    }

    public TransactionBuilder WithNegativeAmount()
    {
        FakerRules.RuleFor(f => f.Amount, faker => faker.Finance.Amount(decimal.MinValue, decimal.MinusOne));
        return this;
    }

    public TransactionBuilder OnDate(DateOnly date)
    {
        FakerRules.RuleFor(f => f.Date, date);
        return this;
    }

    public TransactionBuilder WithNoDate()
    {
        FakerRules.RuleFor(f => f.Date, _ => default);
        return this;
    }

    public TransactionBuilder ForCashflowDate(DateOnly date)
    {
        FakerRules.RuleFor(f => f.CashflowDate, date);
        return this;
    }

    public TransactionBuilder ForAccount(Account entity)
    {
        FakerRules
            .RuleFor(f => f.AccountId, entity.Id)
            // .RuleFor(f => f.Account, entity)
            .RuleFor(f => f.UserId, entity.UserId);
        return this;
    }

    public TransactionBuilder WithNoAccount()
    {
        FakerRules
            .RuleFor(f => f.AccountId, _ => Guid.Empty)
            .RuleFor(f => f.Account, _ => default);
        return this;
    }

    public TransactionBuilder ForCategory(Category entity)
    {
        FakerRules
            .RuleFor(f => f.CategoryId, entity.Id)
            // .RuleFor(f => f.Category, entity)
            ;
        return this;
    }

    public TransactionBuilder WithNoCategory()
    {
        FakerRules
            .RuleFor(f => f.CategoryId, _ => Guid.Empty)
            .RuleFor(f => f.Category, _ => default);
        return this;
    }

    public TransactionBuilder WithNoUser()
    {
        FakerRules.RuleFor(f => f.UserId, _ => Guid.Empty);
        return this;
    }

    public TransactionBuilder WithTags(IEnumerable<string> tags)
    {
        FakerRules.RuleFor(f => f.Tags, tags);
        return this;
    }

    public TransactionBuilder WithNoTags()
    {
        FakerRules.RuleFor(f => f.Tags, _ => Array.Empty<string>());
        return this;
    }
}
