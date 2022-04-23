using System.Net;

using FluentAssertions;

using Holefeeder.Application.Features.StoreItems.Queries;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.FunctionalTests.Infrastructure.Builders;

using Xunit;

using static Holefeeder.FunctionalTests.Infrastructure.Builders.StoreItemEntityBuilder;
using static Holefeeder.FunctionalTests.Infrastructure.MockAuthenticationHandler;

namespace Holefeeder.FunctionalTests.Features.StoreItems;

public class ScenarioGetStoreItems : BaseScenario
{
    private readonly ObjectStoreDatabaseDriver _objectStoreDatabaseDriver;

    public ScenarioGetStoreItems(ApiApplicationDriver apiApplicationDriver) : base(apiApplicationDriver)
    {
        _objectStoreDatabaseDriver = apiApplicationDriver.CreateObjectStoreDatabaseDriver();
        _objectStoreDatabaseDriver.ResetState().Wait();
    }

    [Fact]
    public async Task WhenInvalidRequest()
    {
        GivenUserIsAuthorized();

        await WhenUserTriesToQuery(ApiResources.GetStoreItems, offset: -1);

        ThenShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.");
    }

    [Fact]
    public async Task WhenAuthorizedUser()
    {
        GivenUserIsAuthorized();

        await WhenUserTriesToQuery(ApiResources.GetStoreItems);

        ThenUserShouldBeAuthorizedToAccessEndpoint();
    }

    [Fact]
    public async Task WhenForbiddenUser()
    {
        GivenForbiddenUserIsAuthorized();

        await WhenUserTriesToQuery(ApiResources.GetStoreItems);

        ThenUserShouldBeForbiddenToAccessEndpoint();
    }

    [Fact]
    public async Task WhenUnauthorizedUser()
    {
        GivenUserIsUnauthorized();

        await WhenUserTriesToQuery(ApiResources.GetStoreItems);

        ThenUserShouldNotBeAuthorizedToAccessEndpoint();
    }

    [Fact]
    public async Task WhenAccountsExists()
    {
        const string firstCode = nameof(firstCode);
        const string secondCode = nameof(secondCode);

        await GivenAStoreItem()
            .ForUser(AuthorizedUserId)
            .WithCode(firstCode)
            .SavedInDb(_objectStoreDatabaseDriver);

        await GivenAStoreItem()
            .ForUser(AuthorizedUserId)
            .WithCode(secondCode)
            .SavedInDb(_objectStoreDatabaseDriver);

        await GivenAStoreItem()
            .SavedInDb(_objectStoreDatabaseDriver);

        GivenUserIsAuthorized();

        await WhenUserTriesToQuery(ApiResources.GetStoreItems, sorts: "-code");

        ThenShouldExpectStatusCode(HttpStatusCode.OK);
        var result = HttpClientDriver.DeserializeContent<StoreItemViewModel[]>();
        ThenAssertAll(() =>
        {
            result.Should().NotBeNull().And.HaveCount(2);
            result![0].Code.Should().Be(secondCode);
            result[1].Code.Should().Be(firstCode);
        });
    }
}
