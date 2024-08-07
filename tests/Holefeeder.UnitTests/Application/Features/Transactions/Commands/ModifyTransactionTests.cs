using Holefeeder.Tests.Common.Extensions;

using static Holefeeder.Application.Features.Transactions.Commands.ModifyTransaction;

namespace Holefeeder.UnitTests.Application.Features.Transactions.Commands;

[UnitTest]
public class ModifyTransactionTests
{
    private readonly Faker<Request> _faker = new Faker<Request>()
            .RuleFor(x => x.Id, faker => faker.RandomGuid())
            .RuleFor(x => x.Date, faker => faker.Date.RecentDateOnly())
            .RuleFor(x => x.Amount, faker => faker.Finance.Amount())
            .RuleFor(x => x.Description, faker => faker.Lorem.Sentence())
            .RuleFor(x => x.AccountId, faker => faker.RandomGuid())
            .RuleFor(x => x.CategoryId, faker => faker.RandomGuid())
            .RuleFor(x => x.Tags, faker => faker.Lorem.Words());

    public ModifyTransactionTests() =>
        _faker.RuleFor(x => x.Amount, faker => faker.Finance.Amount(decimal.One, decimal.MaxValue));

    [Fact]
    public async Task GivenValidator_WhenIdIsEmpty_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Id, Guid.Empty).Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Id);
    }

    [Fact]
    public async Task GivenValidator_WhenAccountIdIsEmpty_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.AccountId, Guid.Empty).Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.AccountId);
    }

    [Fact]
    public async Task GivenValidator_WhenCategoryIdIsEmpty_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.CategoryId, Guid.Empty).Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.CategoryId);
    }

    [Fact]
    public async Task GivenValidator_WhenDateIsEmpty_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Date, DateOnly.MinValue).Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Date);
    }

    [Fact]
    public async Task GivenValidator_WhenAmountIsNotGreaterThanZero_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Amount, faker => faker.Finance.Amount(decimal.MinValue, decimal.Zero))
            .Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Amount);
    }

    [Fact]
    public async Task GivenValidator_WhenRequestValid_ThenNoErrors()
    {
        // arrange
        var request = _faker.Generate();

        var validator = new Validator();

        // act
        TestValidationResult<Request>? result = await validator.TestValidateAsync(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
