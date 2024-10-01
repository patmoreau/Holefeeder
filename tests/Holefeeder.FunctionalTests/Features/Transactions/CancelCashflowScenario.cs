using System.Net;
using System.Text.Json;

using DrifterApps.Seeds.Testing.Attributes;
using DrifterApps.Seeds.Testing.Scenarios;

using Holefeeder.Domain.Features.Transactions;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.FunctionalTests.StepDefinitions;
using Holefeeder.Tests.Common;

using static Holefeeder.Tests.Common.Builders.Transactions.CancelCashflowRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Transactions;

[ComponentTest]
[Collection("Api collection")]
public class CancelCashflowScenario(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public Task WhenInvalidRequest() =>
        ScenarioFor("an invalid request is sent", runner =>
            runner.Given(User.IsAuthorized)
                .And(Cashflow.AnInvalidCancelRequest)
                .When(Cashflow.RequestIsSent)
                .Then(ReturnsAnError));

    [Fact]
    public void WhenCancellingACashflow() =>
        ScenarioFor("when user cancels a cashflow", runner =>
            runner.Given(User.IsAuthorized)
                .And(Account.Exists)
                .And(Category.Exists)
                .And(Cashflow.Exists)
                .When(CashflowIsCanceled)
                .Then(CashflowIsInactive));

    private void CashflowIsCanceled(IStepRunner runner) =>
        runner.Execute("a request to cancel a cashflow is made", async () =>
        {
            var cashflow = runner.GetContextData<Cashflow>(CashflowStepDefinition.ContextExistingCashflow);
            cashflow.Should().NotBeNull();

            var request = GivenACancelCashflowRequest().WithId(cashflow.Id).Build();

            var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
            await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.CancelCashflow, json);
        });

    [AssertionMethod]
    private void CashflowIsInactive(IStepRunner runner) =>
        runner.Execute("the cashflow is cancelled", async () =>
        {
            ShouldExpectStatusCode(HttpStatusCode.NoContent);

            var cashflow = runner.GetContextData<Cashflow>(CashflowStepDefinition.ContextExistingCashflow);
            cashflow.Should().NotBeNull();

            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Cashflows.FindAsync(cashflow.Id);

            result.Should().NotBeNull();
            result!.Inactive.Should().BeTrue();
        });

    [AssertionMethod]
    private void ReturnsAnError(IStepRunner runner) => runner.Execute("a validation error is expected",
        () => { ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest); });
}
