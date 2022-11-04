using FluentAssertions;

using Holefeeder.Application.Features.StoreItems.Commands;
using Holefeeder.Application.Features.StoreItems.Queries;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Features;

using Xunit;
using Xunit.Abstractions;

using static Holefeeder.Tests.Common.Builders.StoreItems.CreateStoreItemRequestBuilder;
using static Holefeeder.Tests.Common.Builders.StoreItems.ModifyStoreItemRequestBuilder;

namespace Holefeeder.FunctionalTests.Scenarios;

public class ScenarioStoreItem : BaseScenario<ScenarioStoreItem>
{
    private readonly ObjectStoreDatabaseDriver _objectStoreDatabaseDriver;

    public ScenarioStoreItem(ApiApplicationDriver apiApplicationDriver, ITestOutputHelper testOutputHelper)
        : base(apiApplicationDriver, testOutputHelper)
    {
        if (apiApplicationDriver == null)
        {
            throw new ArgumentNullException(nameof(apiApplicationDriver));
        }

        _objectStoreDatabaseDriver = apiApplicationDriver.CreateObjectStoreDatabaseDriver();
        _objectStoreDatabaseDriver.ResetStateAsync().Wait();
    }

    [Fact]
    public async Task UserCreatesStoreItem()
    {
        Guid id = Guid.Empty;
        CreateStoreItem.Request createRequest = null!;
        StoreItemViewModel storeItem = null!;

        await Given(() => User.IsAuthorized())
            .Given(() => createRequest = GivenACreateStoreItemRequest().Build())
            .When(() => id = StoreItem.GetsCreated(createRequest).WithCreatedId())
            .When(() => storeItem = StoreItem.RetrievedById(id).WithResultAs<StoreItemViewModel>())
            .Then(() =>
            {
                storeItem.Id.Should().Be(id);
                storeItem.Code.Should().Be(createRequest.Code);
                storeItem.Data.Should().Be(createRequest.Data);
            })
            .RunScenarioAsync();
    }

    [Fact]
    public async Task UserModifiesStoreItem()
    {
        Guid id = Guid.Empty;
        ModifyStoreItem.Request request = null!;
        StoreItemViewModel storeItem = null!;

        await Given(() => User.IsAuthorized())
            .When(() => id = StoreItem.GetsCreated().WithCreatedId())
            .Given(() => request = GivenAModifyStoreItemRequest().WithId(id).Build())
            .When(() => storeItem = StoreItem.GetsModified()
                .RetrievedById(id)
                .WithResultAs<StoreItemViewModel>())
            .Then(() =>
            {
                storeItem.Id.Should().NotBeEmpty();
                storeItem.Data.Should().Be(request.Data);
            })
            .RunScenarioAsync();
    }
}