using DrifterApps.Seeds.Testing;

using Holefeeder.Domain.Features.Accounts;

using static Holefeeder.Application.Features.Accounts.Commands.OpenAccount;

namespace Holefeeder.Tests.Common.Builders.Accounts;

internal class OpenAccountRequestBuilder : FakerBuilder<Request>
{
    protected override Faker<Request> FakerRules { get; } = new Faker<Request>()
        .CustomInstantiator(faker =>
            new Request(faker.PickRandom<AccountType>(AccountType.List),
                faker.Lorem.Word(), faker.Date.PastDateOnly(), faker.Finance.Amount(), faker.Lorem.Sentence()))
        .RuleFor(x => x.Type, faker => faker.PickRandom<AccountType>(AccountType.List))
        .RuleFor(x => x.Name, faker => faker.Lorem.Word())
        .RuleFor(x => x.OpenBalance, faker => faker.Finance.Amount())
        .RuleFor(x => x.Description, faker => faker.Lorem.Sentence())
        .RuleFor(x => x.OpenDate, faker => faker.Date.PastDateOnly());

    public OpenAccountRequestBuilder WithName(string name)
    {
        FakerRules.RuleFor(x => x.Name, name);
        return this;
    }

    public OpenAccountRequestBuilder WithNoName()
    {
        FakerRules.RuleFor(x => x.Name, string.Empty);
        return this;
    }

    public static OpenAccountRequestBuilder GivenAnOpenAccountRequest() => new();

    public static OpenAccountRequestBuilder GivenAnInvalidOpenAccountRequest() =>
        new OpenAccountRequestBuilder().WithNoName();
}
