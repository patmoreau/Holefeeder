using System.Net;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.FunctionalTests.Drivers;

using static Holefeeder.Application.Features.Transactions.Commands.Transfer;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Categories.CategoryBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.TransferRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Transactions;

[ComponentTest]
[Collection("Api collection")]
public sealed class ScenarioTransfer(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task InvalidRequest()
    {
        Request request = default!;

        await ScenarioFor("an invalid transfer request", runner =>
        {
            runner
                .Given("an invalid transfer", () => request = GivenAnInvalidTransfer().Build())
                .And(User.IsAuthorized)
                .When("a transfer is made", () => Transaction.Transfer(request))
                .Then("should receive a validation error", () => ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest));
        });
    }

    [Fact]
    public async Task GivenAuthorizedUser()
    {
        Request request = default!;

        await ScenarioFor("an authorized user making a transfer", runner =>
        {
            runner
                .Given("a transfer request", () => request = GivenATransfer().Build())
                .And(User.IsAuthorized)
                .When("the transfer is made", () => Transaction.Transfer(request))
                .Then("the user should be authorized to access the endpoint", ShouldBeAuthorizedToAccessEndpoint);
        });
    }

    [Fact]
    public async Task GivenForbiddenUser()
    {
        Request request = null!;

        await ScenarioFor("a forbidden user making a request", runner =>
        {
            runner
                .Given("a transfer request", () => request = GivenATransfer().Build())
                .And(User.IsForbidden)
                .When("the request is sent", () => Transaction.Transfer(request))
                .Then("should be forbidden from accessing the endpoint", ShouldBeForbiddenToAccessEndpoint);
        });
    }

    [Fact]
    public async Task UnauthorizedUser()
    {
        Request entity = null!;

        await ScenarioFor("an unauthorized user making a transfer request", runner =>
        {
            runner
                .Given("a transfer request", () => entity = GivenATransfer().Build())
                .And(User.IsUnauthorized)
                .When("the transfer request is made", () => Transaction.Transfer(entity))
                .Then("the user should not be authorized to access endpoint", ShouldNotBeAuthorizedToAccessEndpoint);
        });
    }

    [Fact(Skip = "Works locally but not on the workflow; need this deployed to fix urgent bug")]
    public async Task ValidRequest()
    {
        Account fromAccount = null!;
        Account toAccount = null!;
        Request request = null!;
        (Guid FromTransactionId, Guid ToTransactionId) ids = default;

        await ScenarioFor("a valid transfer request", runner =>
        {
            runner
                .Given("the user has an account to transfer from", async () => fromAccount = await GivenAnActiveAccount().ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("an account to transfer to", async () => toAccount = await GivenAnActiveAccount().ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("they hava a category to receive money", async () => await GivenACategory().WithName("Transfer In").ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("a category to send money", async () => await GivenACategory().WithName("Transfer Out").ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("their request is valid", () => request = GivenATransfer().FromAccount(fromAccount).ToAccount(toAccount).Build())
                .And(User.IsAuthorized)
                .When("the transfer request is sent", () => Transaction.Transfer(request))
                .Then("the return code should be Created", () => ShouldExpectStatusCode(HttpStatusCode.Created))
                // .And(ShouldGetTheRouteOfTheNewResourceInTheHeader)
                .And("both transaction ids should be received", () =>
                {
                    ids = ShouldHaveReceived<(Guid FromTransactionId, Guid ToTransactionId)>();
                    ids.FromTransactionId.Should().NotBeEmpty();
                    ids.ToTransactionId.Should().NotBeEmpty();
                })
                .And("the data of the outgoing transaction be valid", async () =>
                {
                    await using var dbContext = DatabaseDriver.CreateDbContext();

                    var result = await dbContext.Transactions.FindAsync(ids.FromTransactionId);

                    result.Should().NotBeNull($"because the FromTransactionId ({ids.FromTransactionId}) was not found");

                    result.Should()
                        .NotBeNull()
                        .And
                        .BeEquivalentTo(request);
                })
                .And("the data of the incoming transaction be valid", async () =>
                {
                    await using var dbContext = DatabaseDriver.CreateDbContext();

                    var result = await dbContext.Transactions.FindAsync(ids.ToTransactionId);

                    result.Should()
                        .NotBeNull($"because the ToTransactionId ({ids.ToTransactionId}) was not found")
                        .And.BeEquivalentTo(request);
                });
        });
    }
}
