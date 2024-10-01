using System.Net;
using System.Text.Json;

using DrifterApps.Seeds.Testing.Attributes;
using DrifterApps.Seeds.Testing.Scenarios;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.FunctionalTests.StepDefinitions;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.Accounts.Commands.CloseAccount;
using static Holefeeder.Tests.Common.Builders.Accounts.CloseAccountRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
[Collection("Api collection")]
public class CloseAccountScenario(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest() => await ScenarioFor("closing an account with an invalid request", runner =>
        runner
            .Given(User.IsAuthorized)
            .When(TheAccountIsClosedWithAnInvalidRequest)
            .Then(AValidationErrorShouldBeReceived));

    [Fact]
    public async Task WhenAccountNotFound() => await ScenarioFor("closing an account that does not exists", runner =>
        runner
            .Given(User.IsAuthorized)
            .When(AnAccountThatDoesNotExistIsClosed)
            .Then(TheAccountShouldNotBeFound));

    [Fact]
    public Task WhenCloseAccount() => ScenarioFor("closing an account", runner =>
        runner
            .Given(User.IsAuthorized)
            .And(Account.Exists)
            .When(TheAccountIsClosed)
            .Then(TheAccountShouldBeClosed));

    private void TheAccountIsClosedWithAnInvalidRequest(IStepRunner runner) =>
        runner.Execute("the account is closed with an invalid request", async () =>
        {
            var request = GivenAnInvalidCloseAccountRequest().Build();
            await SendRequest(request);
        });

    private void AnAccountThatDoesNotExistIsClosed(IStepRunner runner) =>
        runner.Execute("an account that does not exist is closed", async () =>
        {
            var request = GivenACloseAccountRequest().Build();
            await SendRequest(request);
        });

    private void TheAccountIsClosed(IStepRunner runner) =>
        runner.Execute("the account is closed", async () =>
        {
            var account = runner.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
            account.Should().NotBeNull();

            var request = GivenACloseAccountRequest().WithId(account.Id).Build();
            await SendRequest(request);
        });

    [AssertionMethod]
    private void AValidationErrorShouldBeReceived(IStepRunner runner) =>
        runner.Execute("should receive a validation error",
            () => ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest));

    [AssertionMethod]
    private void TheAccountShouldNotBeFound(IStepRunner runner) => runner.Execute("the account should not be found", () => ShouldExpectStatusCode(HttpStatusCode.NotFound));

    [AssertionMethod]
    private void TheAccountShouldBeClosed(IStepRunner runner) =>
        runner.Execute("the account should be closed", async () =>
        {
            ShouldExpectStatusCode(HttpStatusCode.NoContent);

            var account = runner.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
            account.Should().NotBeNull();

            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Accounts.FindAsync(account.Id);
            result.Should().NotBeNull();
            result!.Inactive.Should().BeTrue();
        });

    private async Task SendRequest(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.CloseAccount, json);
    }
}
