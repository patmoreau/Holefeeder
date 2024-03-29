using DrifterApps.Seeds.Application;

using Microsoft.Extensions.Logging;

namespace Holefeeder.UnitTests;

public static class MockHelper
{
    public static ILogger<T> CreateLogger<T>() => Substitute.For<ILogger<T>>();

    public static IUserContext CreateUserContext()
    {
        var userContextMock = Substitute.For<IUserContext>();
        userContextMock.Id.Returns(Fakerizer.Random.Guid());
        return userContextMock;
    }
}
