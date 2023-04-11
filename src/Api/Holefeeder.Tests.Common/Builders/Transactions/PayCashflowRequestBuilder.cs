using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Tests.Common.SeedWork;
using static Holefeeder.Application.Features.Transactions.Commands.PayCashflow;

namespace Holefeeder.Tests.Common.Builders.Transactions;

internal class PayCashflowRequestBuilder : RootBuilder<Request>
{
    public PayCashflowRequestBuilder ForCashflow(Cashflow cashflow)
    {
        Faker.RuleFor(x => x.CashflowId, cashflow.Id);
        return this;
    }

    public PayCashflowRequestBuilder ForDate(DateTime cashflowDate)
    {
        Faker.RuleFor(x => x.CashflowDate, cashflowDate);
        return this;
    }

    public static PayCashflowRequestBuilder GivenACashflowPayment() => new();

    public static PayCashflowRequestBuilder GivenAnInvalidCashflowPayment()
    {
        PayCashflowRequestBuilder builder = new PayCashflowRequestBuilder();
        builder.Faker.RuleFor(x => x.CashflowId, Guid.Empty);
        return builder;
    }
}
