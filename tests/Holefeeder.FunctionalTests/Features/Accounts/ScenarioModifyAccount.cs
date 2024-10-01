using System.Net;
using System.Text.Json;

using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.Accounts.Commands.ModifyAccount;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Accounts.ModifyAccountRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioModifyAccount(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest()
    {
        var entity = GivenAnInvalidModifyAccountRequest()
            .Build();

        GivenUserIsAuthorized();

        await WhenUserModifiesAccount(entity);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAccountNotFound()
    {
        var request = GivenAModifyAccountRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserModifiesAccount(request);

        ShouldExpectStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenModifyAccount()
    {
        var entity = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAModifyAccountRequest()
            .WithId(entity.Id)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserModifiesAccount(request);

        ShouldExpectStatusCode(HttpStatusCode.NoContent);
        await using var dbContext = DatabaseDriver.CreateDbContext();

        var result = await dbContext.Accounts.FindAsync(entity.Id);
        result.Should()
            .NotBeNull()
            .And
            .BeEquivalentTo(request);
    }

    private async Task WhenUserModifiesAccount(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.ModifyAccount, json);
    }
}
