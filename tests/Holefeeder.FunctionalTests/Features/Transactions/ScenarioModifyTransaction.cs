using System.Net;
using System.Text.Json;

using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.Transactions.Commands.ModifyTransaction;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Categories.CategoryBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.ModifyTransactionRequestBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.TransactionBuilder;

namespace Holefeeder.FunctionalTests.Features.Transactions;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioModifyTransaction(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper) : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest()
    {
        var request = GivenAnInvalidModifyTransactionRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserModifiedATransaction(request);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAccountDoesNotExists()
    {
        var request = GivenAModifyTransactionRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserModifiedATransaction(request);

        ShouldReceiveProblemDetailsWithErrorMessage(HttpStatusCode.BadRequest,
            $"Account '{request.AccountId.Value}' does not exists.");
    }

    [Fact]
    public async Task WhenCategoryDoesNotExists()
    {
        var account = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAModifyTransactionRequest()
            .WithAccount(account).Build();

        GivenUserIsAuthorized();

        await WhenUserModifiedATransaction(request);

        ShouldReceiveProblemDetailsWithErrorMessage(HttpStatusCode.BadRequest,
            $"Category '{request.CategoryId.Value}' does not exists.");
    }

    [Fact]
    public async Task WhenTransactionDoesNotExists()
    {
        var account = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var category = await GivenACategory()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAModifyTransactionRequest()
            .WithAccount(account)
            .WithCategory(category)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserModifiedATransaction(request);

        ShouldReceiveProblemDetailsWithErrorMessage(HttpStatusCode.NotFound,
            $"Transaction '{request.Id.Value}' not found");
    }

    [Fact]
    public async Task WhenModifyATransaction()
    {
        var accounts = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .CollectionSavedInDbAsync(DatabaseDriver, 2);

        var categories = await GivenACategory()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .CollectionSavedInDbAsync(DatabaseDriver, 2);

        var transaction = await GivenATransaction()
            .ForAccount(accounts.ElementAt(0))
            .ForCategory(categories.ElementAt(0))
            .SavedInDbAsync(DatabaseDriver);

        GivenUserIsAuthorized();

        var request = GivenAModifyTransactionRequest()
            .WithId(transaction.Id)
            .WithAccount(accounts.ElementAt(1))
            .WithCategory(categories.ElementAt(1))
            .Build();

        await WhenUserModifiedATransaction(request);

        ShouldExpectStatusCode(HttpStatusCode.NoContent);

        await using var dbContext = DatabaseDriver.CreateDbContext();

        var result = await dbContext.Transactions.FindAsync(transaction.Id);

        result.Should().NotBeNull().And.BeEquivalentTo(request);
    }

    private async Task WhenUserModifiedATransaction(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.ModifyTransaction, json);
    }
}
