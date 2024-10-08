using System.Net;

using Holefeeder.Application.Features.Accounts.Queries;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;

using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Categories.CategoryBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.TransactionBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioGetAccount(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenNotFound()
    {
        GivenUserIsAuthorized();

        await WhenUserGetAccount(Guid.NewGuid());

        ShouldExpectStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenInvalidRequest()
    {
        GivenUserIsAuthorized();

        await WhenUserGetAccount(Guid.Empty);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAccountExistsWithExpenses()
    {
        var account = await GivenAnActiveAccount()
            .OfType(AccountType.Checking)
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var category = await GivenACategory()
            .OfType(CategoryType.Expense)
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var transaction = await GivenATransaction()
            .ForAccount(account)
            .ForCategory(category)
            .SavedInDbAsync(DatabaseDriver);

        GivenUserIsAuthorized();

        await WhenUserGetAccount(account.Id);

        ShouldExpectStatusCode(HttpStatusCode.OK);
        var result = HttpClientDriver.DeserializeContent<AccountViewModel>();
        AssertAll(() =>
        {
            result.Should()
                .NotBeNull()
                .And
                .BeEquivalentTo(new
                {
                    Id = (Guid)account.Id,
                    account.Type,
                    account.Name,
                    OpenBalance = (decimal)account.OpenBalance,
                    account.OpenDate,
                    TransactionCount = 1,
                    Balance = decimal.Subtract(account.OpenBalance, transaction.Amount),
                    Updated = transaction.Date,
                    account.Description,
                    account.Favorite,
                    account.Inactive
                });
        });
    }

    [Fact]
    public async Task WhenAccountExistsWithGains()
    {
        var account = await GivenAnActiveAccount()
            .OfType(AccountType.Checking)
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var category = await GivenACategory()
            .OfType(CategoryType.Gain)
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var transaction = await GivenATransaction()
            .ForAccount(account)
            .ForCategory(category)
            .SavedInDbAsync(DatabaseDriver);

        GivenUserIsAuthorized();

        await WhenUserGetAccount(account.Id);

        ShouldExpectStatusCode(HttpStatusCode.OK);
        var result = HttpClientDriver.DeserializeContent<AccountViewModel>();
        AssertAll(() =>
        {
            result.Should()
                .NotBeNull()
                .And
                .BeEquivalentTo(new
                {
                    Id = (Guid)account.Id,
                    account.Type,
                    account.Name,
                    OpenBalance = (decimal)account.OpenBalance,
                    account.OpenDate,
                    TransactionCount = 1,
                    Balance = decimal.Add(account.OpenBalance, transaction.Amount),
                    Updated = transaction.Date,
                    account.Description,
                    account.Favorite,
                    account.Inactive
                });
        });
    }

    private async Task WhenUserGetAccount(Guid id) => await HttpClientDriver.SendRequestAsync(ApiResources.GetAccount, id.ToString());
}
