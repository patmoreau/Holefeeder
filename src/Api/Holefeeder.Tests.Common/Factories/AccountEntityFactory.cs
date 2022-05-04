using AutoBogus;

using Bogus.Extensions;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.SeedWork;
using Holefeeder.Infrastructure.Entities;

namespace Holefeeder.Tests.Common.Factories;

internal sealed class AccountEntityFactory : AutoFaker<AccountEntity>
{
    private const decimal OPEN_BALANCE_MAX = 10000m;

    public AccountEntityFactory(bool inactive = false)
    {
        RuleFor(x => x.Id, faker => faker.Random.Guid());
        RuleFor(x => x.Type, faker => faker.PickRandom(Enumeration.GetAll<AccountType>()));
        RuleFor(x => x.Name, faker => faker.Lorem.Random.String(minLength:1, maxLength: 255));
        RuleFor(x => x.Favorite, faker => faker.Random.Bool());
        RuleFor(x => x.OpenBalance, faker => faker.Finance.Amount(max: OPEN_BALANCE_MAX));
        RuleFor(x => x.OpenDate, faker => faker.Date.Past().Date);
        RuleFor(x => x.Description, faker => faker.Random.Words());
        RuleFor(x => x.Inactive, _ => inactive);
        RuleFor(x => x.UserId, faker => faker.Random.Guid());
    }
}
