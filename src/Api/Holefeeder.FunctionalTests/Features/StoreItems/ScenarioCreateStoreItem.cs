using System.Net;
using System.Text.Json;
using Holefeeder.Domain.Features.StoreItem;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using static Holefeeder.Application.Features.StoreItems.Commands.CreateStoreItem;
using static Holefeeder.Tests.Common.Builders.StoreItems.CreateStoreItemRequestBuilder;
using static Holefeeder.Tests.Common.Builders.StoreItems.StoreItemBuilder;

namespace Holefeeder.FunctionalTests.Features.StoreItems;

[ComponentTest]
public class ScenarioCreateStoreItem : HolefeederScenario
{
    public ScenarioCreateStoreItem(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
        : base(applicationDriver, testOutputHelper)
    {
    }

    [Fact]
    public async Task WhenInvalidRequest()
    {
        Request storeItem = GivenACreateStoreItemRequest()
            .WithCode(string.Empty)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserCreateStoreItem(storeItem);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.");
    }

    [Fact]
    public async Task WhenCreateStoreItem()
    {
        Request storeItem = GivenACreateStoreItemRequest().Build();

        GivenUserIsAuthorized();

        await WhenUserCreateStoreItem(storeItem);

        ShouldExpectStatusCode(HttpStatusCode.Created);

        ShouldGetTheRouteOfTheNewResourceInTheHeader();
    }

    [Fact]
    public async Task WhenCodeAlreadyExist()
    {
        StoreItem existingItem = await GivenAStoreItem()
            .SavedInDbAsync(DatabaseDriver);

        Request storeItem = GivenACreateStoreItemRequest()
            .WithCode(existingItem.Code)
            .Build();

        GivenUserIsAuthorized();

        await WhenUserCreateStoreItem(storeItem);

        ShouldExpectStatusCode(HttpStatusCode.BadRequest);
    }

    private async Task WhenUserCreateStoreItem(Request request)
    {
        string json = JsonSerializer.Serialize(request);
        await HttpClientDriver.SendPostRequestAsync(ApiResources.CreateStoreItem, json);
    }
}
