using System.Net;
using System.Text.Json;
using DrifterApps.Seeds.Testing.Attributes;
using DrifterApps.Seeds.Testing.Scenarios;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.FunctionalTests.StepDefinitions;
using Microsoft.EntityFrameworkCore;
using static Holefeeder.Application.Features.Accounts.Commands.CloseAccount;
using static Holefeeder.Tests.Common.Builders.Accounts.CloseAccountRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
public class CloseAccountScenario : HolefeederScenario
{
    public CloseAccountScenario(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
        : base(applicationDriver, testOutputHelper)
    {
    }

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

    private void TheAccountIsClosedWithAnInvalidRequest(IStepRunner runner)
    {
        runner.Execute("the account is closed with an invalid request", async () =>
        {
            Request request = GivenAnInvalidCloseAccountRequest().Build();
            await SendRequest(request);
        });
    }

    private void AnAccountThatDoesNotExistIsClosed(IStepRunner runner)
    {
        runner.Execute("an account that does not exist is closed", async () =>
        {
            Request request = GivenACloseAccountRequest().Build();
            await SendRequest(request);
        });
    }

    private void TheAccountIsClosed(IStepRunner runner)
    {
        runner.Execute("the account is closed", async () =>
        {
            var account = runner.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
            account.Should().NotBeNull();

            Request request = GivenACloseAccountRequest().WithId(account.Id).Build();
            await SendRequest(request);
        });
    }

    [AssertionMethod]
    private void AValidationErrorShouldBeReceived(IStepRunner runner)
    {
        runner.Execute("should receive a validation error",
            () => ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred."));
    }

    [AssertionMethod]
    private void TheAccountShouldNotBeFound(IStepRunner runner)
    {
        runner.Execute("the account should not be found", () => ShouldExpectStatusCode(HttpStatusCode.NotFound));
    }

    [AssertionMethod]
    private void TheAccountShouldBeClosed(IStepRunner runner)
    {
        runner.Execute("the account should be closed", async () =>
        {
            ShouldExpectStatusCode(HttpStatusCode.NoContent);

            var account = runner.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
            account.Should().NotBeNull();

            using var dbContext = DatabaseDriver.CreateDbContext();

            Account? result = await dbContext.FindByIdAsync<Account>(account.Id);
            result.Should().NotBeNull();
            result!.Inactive.Should().BeTrue();
        });
    }

    private async Task SendRequest(Request request)
    {
        string json = JsonSerializer.Serialize(request);
        await HttpClientDriver.SendPostRequestAsync(ApiResources.CloseAccount, json);
    }
}
