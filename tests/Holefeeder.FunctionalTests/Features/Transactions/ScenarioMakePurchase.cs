using System.Net;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.StepDefinitions;

using static Holefeeder.Application.Features.Transactions.Commands.MakePurchase;
using static Holefeeder.Tests.Common.Builders.Transactions.MakePurchaseRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Transactions;

[ComponentTest]
[Collection("Api collection")]
public sealed class ScenarioMakePurchase(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper) : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task InvalidRequest()
    {
        Request request = default!;

        await ScenarioFor("making an invalid request purchase", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And("making a purchase with no date", () => request = GivenAPurchase().WithNoDate().Build())
                .When("sending the request", () => Transaction.MakesPurchase(request))
                .Then("should receive a validation error", () => ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest));
        });
    }

    [Fact]
    public async Task AuthorizedUser()
    {
        Request request = null!;

        await ScenarioFor("making an authorized purchase", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And("making a purchase", () => request = GivenAPurchase().Build())
                .When("sending the request", () => Transaction.MakesPurchase(request))
                .Then("should be allowed", ShouldBeAuthorizedToAccessEndpoint);
        });
    }

    [Fact]
    public async Task ForbiddenUser()
    {
        Request request = null!;

        await ScenarioFor("making an forbidden purchase", player =>
        {
            player
                .Given(User.IsForbidden)
                .And("making a purchase", () => request = GivenAPurchase().Build())
                .When("sending the request", () => Transaction.MakesPurchase(request))
                .Then("should be forbidden", ShouldBeForbiddenToAccessEndpoint);
        });
    }

    [Fact]
    public async Task UnauthorizedUser()
    {
        Request entity = null!;

        await ScenarioFor("making an unauthorized purchase", player =>
        {
            player
                .Given(User.IsUnauthorized)
                .And("making a purchase", () => entity = GivenAPurchase().Build())
                .When("sending the request", () => Transaction.MakesPurchase(entity))
                .Then("should be unauthorized", ShouldNotBeAuthorizedToAccessEndpoint);
        });
    }

    // [Fact(Skip = "Works locally but not on the workflow; need this deployed to fix urgent bug")]
    [Fact]
    public async Task ValidRequest()
    {
        Request request = null!;

        await ScenarioFor("making a valid purchase", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And(Account.Exists)
                .And(Category.Exists)
                .And("wanting to make a purchase", () =>
                {
                    var account = player.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
                    var category = player.GetContextData<Category>(CategoryStepDefinition.ContextExistingCategory);

                    request = GivenAPurchase().ForAccount(account).ForCategory(category).Build();
                })
                .When("the purchase is made", () => Transaction.MakesPurchase(request))
                .Then("the response should be created", () => ShouldExpectStatusCode(HttpStatusCode.Created))
                .And("the purchase saved in the database should match the request", async () =>
                {
                    var location = ShouldGetTheRouteOfTheNewResourceInTheHeader();
                    var id = ResourceIdFromLocation(location);

                    await using var dbContext = DatabaseDriver.CreateDbContext();

                    var result = await dbContext.Transactions.FindAsync(TransactionId.Create(id));

                    result.Should()
                        .NotBeNull($"because the TransactionId ({id}) was not found")
                        .And.BeEquivalentTo(request);
                });
        });
    }
}
