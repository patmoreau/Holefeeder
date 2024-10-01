using System.Net;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.FunctionalTests.Drivers;

using static Holefeeder.Application.Features.Transactions.Commands.PayCashflow;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Categories.CategoryBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.CashflowBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.PayCashflowRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Transactions;

[ComponentTest]
[Collection("Api collection")]
public sealed class ScenarioPayCashflow(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper) : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task InvalidRequest()
    {
        Request request = default!;

        await ScenarioFor("trying to pay a cashflow with an invalid request", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And("creates an invalid payment", () => request = GivenAnInvalidCashflowPayment().Build())
                .When("sending the request", () => Transaction.PayACashflow(request))
                .Then("should receive a validation error", () => ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest));
        });
    }

    [Fact]
    public async Task GivenAuthorizedUser()
    {
        Request request = null!;

        await ScenarioFor("an authorized user pays a cashflow", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And("a cashflow payment request", () => request = GivenACashflowPayment().Build())
                .When("the payment is sent", () => Transaction.PayACashflow(request))
                .Then("the user should be authorized", ShouldBeAuthorizedToAccessEndpoint);
        });
    }

    [Fact]
    public async Task GivenForbiddenUser()
    {
        Request request = null!;

        await ScenarioFor("a forbidden user pays a cashflow", player =>
        {
            player
                .Given(User.IsForbidden)
                .And("a cashflow payment request", () => request = GivenACashflowPayment().Build())
                .When("the payment is sent", () => Transaction.PayACashflow(request))
                .Then("the user should be forbidden", ShouldBeForbiddenToAccessEndpoint);
        });
    }

    [Fact]
    public async Task UnauthorizedUser()
    {
        Request entity = null!;

        await ScenarioFor("an unauthorized user pays a cashflow", player =>
        {
            player
                .Given(User.IsUnauthorized)
                .And("a cashflow payment request", () => entity = GivenACashflowPayment().Build())
                .When("the payment is sent", () => Transaction.PayACashflow(entity))
                .Then("the user should not be authorized", ShouldNotBeAuthorizedToAccessEndpoint);
        });
    }

    [Fact]
    public async Task ValidRequest()
    {
        Account account = null!;
        Category category = null!;
        Cashflow cashflow = null!;
        Request request = null!;

        await ScenarioFor("paying a cashflow", player =>
        {
            player
                .Given(User.IsAuthorized)
                .And("has an active account", async () => account = await GivenAnActiveAccount().ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("a category", async () => category = await GivenACategory().ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("a cashflow setup", async () => cashflow = await GivenAnActiveCashflow().ForAccount(account).ForCategory(category).ForUser(TestUsers[AuthorizedUser].UserId).SavedInDbAsync(DatabaseDriver))
                .And("and wanting to pay a cashflow", () => request = GivenACashflowPayment().ForCashflow(cashflow).ForDate(cashflow.EffectiveDate).Build())
                .When("the payment is made", () => Transaction.PayACashflow(request))
                .Then("the response should be created", () => ShouldExpectStatusCode(HttpStatusCode.Created))
                .And("the cashflow paid saved in the database should match the request", async () =>
                {
                    var location = ShouldGetTheRouteOfTheNewResourceInTheHeader();
                    var id = ResourceIdFromLocation(location);

                    await using var dbContext = DatabaseDriver.CreateDbContext();

                    var result = await dbContext.Transactions.FindAsync(TransactionId.Create(id));

                    result.Should().NotBeNull($"because the TransactionId ({id}) was not found")
                        .And.BeEquivalentTo(request);
                });
        });
    }
}
