using DrifterApps.Seeds.Testing;

using Holefeeder.Tests.Common.Extensions;

using static Holefeeder.Application.Features.Accounts.Commands.ModifyAccount;

namespace Holefeeder.Tests.Common.Builders.Accounts;

internal class ModifyAccountRequestBuilder : FakerBuilder<Request>
{
    protected override Faker<Request> FakerRules { get; } = new Faker<Request>()
        .CustomInstantiator(faker =>
            new Request(faker.RandomGuid(), faker.Lorem.Word(), faker.Finance.Amount(), faker.Lorem.Sentence()))
        .RuleFor(x => x.Id, faker => faker.RandomGuid())
        .RuleFor(x => x.Name, faker => faker.Lorem.Word())
        .RuleFor(x => x.OpenBalance, faker => faker.Finance.Amount())
        .RuleFor(x => x.Description, faker => faker.Lorem.Sentence());

    public ModifyAccountRequestBuilder WithId(Guid id)
    {
        FakerRules.RuleFor(x => x.Id, id);
        return this;
    }

    public ModifyAccountRequestBuilder WithNoId()
    {
        FakerRules.RuleFor(x => x.Id, Guid.Empty);
        return this;
    }

    public static ModifyAccountRequestBuilder GivenAModifyAccountRequest() => new();

    public static ModifyAccountRequestBuilder GivenAnInvalidModifyAccountRequest() =>
        new ModifyAccountRequestBuilder().WithNoId();
}
