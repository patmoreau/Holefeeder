using System.Net;
using System.Text.Json;

using Holefeeder.Domain.Features.StoreItem;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.StoreItems.Commands.ModifyStoreItem;
using static Holefeeder.Tests.Common.Builders.StoreItems.ModifyStoreItemRequestBuilder;
using static Holefeeder.Tests.Common.Builders.StoreItems.StoreItemBuilder;

namespace Holefeeder.FunctionalTests.Features.StoreItems;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioModifyStoreItem(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest()
    {
        var storeItem = GivenAModifyStoreItemRequest()
            .WithId(StoreItemId.Empty)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserModifyStoreItem(storeItem);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenModifyStoreItem()
    {
        var storeItem = await GivenAStoreItem()
            .ForUser(TestUsers[AuthorizedUser].UserId)
            .SavedInDbAsync(DatabaseDriver);

        var request = GivenAModifyStoreItemRequest()
            .WithId(storeItem.Id)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserModifyStoreItem(request);

        ShouldExpectStatusCode(HttpStatusCode.NoContent);

        await using var dbContext = DatabaseDriver.CreateDbContext();

        var result = await dbContext.StoreItems.FindAsync(storeItem.Id);
        result.Should().NotBeNull();
        result!.Data.Should().BeEquivalentTo(request.Data);
    }

    private async Task WhenUserModifyStoreItem(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.ModifyStoreItem, json);
    }
}
