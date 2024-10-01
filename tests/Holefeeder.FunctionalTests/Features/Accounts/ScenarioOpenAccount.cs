using System.Net;
using System.Text.Json;

using DrifterApps.Seeds.Testing.Scenarios;

using Holefeeder.Domain.Features.Accounts;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.Accounts.Commands.OpenAccount;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Accounts.OpenAccountRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioOpenAccount(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    private const string OpenAccountRequestKey = $"{nameof(ScenarioOpenAccount)}_OpenAccountRequest";

    [Fact]
    public async Task WhenInvalidRequest()
    {
        var request = GivenAnInvalidOpenAccountRequest()
            .Build();

        GivenUserIsAuthorized();

        await WhenUserOpensAnAccount(request);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAccountNameAlreadyExistsRequest()
    {
        var entity = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAnOpenAccountRequest()
            .WithName(entity.Name)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserOpensAnAccount(request);

        ShouldExpectStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenOpenAccount() => await ScenarioFor("opening an account", runner =>
        runner.Given(User.IsAuthorized)
            .When(OpeningAccount)
            .Then(AccountShouldBeOpened));

    private void OpeningAccount(IStepRunner runner) =>
        runner.Execute("opening an account", async () =>
        {
            var request = GivenAnOpenAccountRequest().Build();

            runner.SetContextData(OpenAccountRequestKey, request);

            await WhenUserOpensAnAccount(request);
        });

    private async Task WhenUserOpensAnAccount(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.OpenAccount, json);
    }

    private void AccountShouldBeOpened(IStepRunner runner) =>
        runner.Execute("the account was created successfully", async () =>
        {
            ShouldExpectStatusCode(HttpStatusCode.Created);

            var location = ShouldGetTheRouteOfTheNewResourceInTheHeader();
            var id = ResourceIdFromLocation(location);

            var accountId = AccountId.Create(id);
            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Accounts.FindAsync(accountId);

            var request = runner.GetContextData<Request>(OpenAccountRequestKey);

            result.Should().NotBeNull().And.BeEquivalentTo(request);
        });
}
