using static Holefeeder.Application.Features.Accounts.Commands.FavoriteAccount;

namespace Holefeeder.UnitTests.Application.Features.Accounts.Commands;

public class FavoriteAccountTests
{
    private readonly Faker<Request> _faker;

    public FavoriteAccountTests()
    {
        _faker = new AutoFaker<Request>();
    }

    [Fact]
    public void GivenValidator_WhenIdIsEmpty_ThenValidationError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Id, _ => Guid.Empty).Generate();

        var validator = new Validator();

        // act
        var result = validator.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Id);
    }
}
