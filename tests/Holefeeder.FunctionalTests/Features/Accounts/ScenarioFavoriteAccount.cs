using System.Net;
using System.Text.Json;

using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.Accounts.Commands.FavoriteAccount;
using static Holefeeder.Tests.Common.Builders.Accounts.AccountBuilder;
using static Holefeeder.Tests.Common.Builders.Accounts.FavoriteAccountRequestBuilder;

namespace Holefeeder.FunctionalTests.Features.Accounts;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioFavoriteAccount(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest()
    {
        var request = GivenAnInvalidFavoriteAccountRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserSetsFavoriteAccount(request);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAccountNotFound()
    {
        var request = GivenAFavoriteAccountRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserSetsFavoriteAccount(request);

        ShouldExpectStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenFavoriteAccount()
    {
        var entity = await GivenAnActiveAccount()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .IsFavorite(false)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAFavoriteAccountRequest()
            .WithId(entity.Id)
            .IsFavorite()
            .Build();

        GivenUserIsAuthorized();

        await WhenUserSetsFavoriteAccount(request);

        ShouldExpectStatusCode(HttpStatusCode.NoContent);

        await using var dbContext = DatabaseDriver.CreateDbContext();

        var result = await dbContext.Accounts.FindAsync(entity.Id);
        result.Should().NotBeNull();
        result!.Favorite.Should().BeTrue();
    }

    private async Task WhenUserSetsFavoriteAccount(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.FavoriteAccount, json);
    }
}
