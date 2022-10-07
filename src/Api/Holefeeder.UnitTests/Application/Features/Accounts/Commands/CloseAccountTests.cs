using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AutoBogus;

using Bogus;

using FluentAssertions;
using FluentAssertions.Execution;

using FluentValidation.TestHelper;

using Holefeeder.Application.Features.Accounts.CloseAccount;
using Holefeeder.Application.Features.Accounts.Exceptions;
using Holefeeder.Application.SeedWork;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Tests.Common.Factories;

using MediatR;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Xunit;

namespace Holefeeder.UnitTests.Application.Features.Accounts.Commands;

public class CloseAccountTests
{
    private readonly AccountFactory _accountFactory = new();
    private readonly Faker<Request> _faker;
    private readonly IAccountRepository _repositoryMock = Substitute.For<IAccountRepository>();

    private readonly IUserContext _userContextMock = MockHelper.CreateUserContext();

    public CloseAccountTests()
    {
        _faker = new AutoFaker<Request>().RuleForType(typeof(AccountType),
            faker => faker.PickRandom(AccountType.List.ToArray()));
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

    [Fact]
    public async Task GivenHandler_WhenIdNotFound_ThenThrowException()
    {
        // arrange
        var request = _faker.Generate();

        var handler = new Handler(_userContextMock, _repositoryMock);

        // act
        Func<Task> action = () => handler.Handle(request, default);

        // assert
        await action.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task GivenHandler_WhenRequestValid_ThenReturnId()
    {
        // arrange
        var request = _faker.Generate();
        _repositoryMock.FindByIdAsync(Arg.Is(request.Id), Arg.Is(_userContextMock.UserId), Arg.Any<CancellationToken>())
            .Returns(_accountFactory.Generate());

        var handler = new Handler(_userContextMock, _repositoryMock);

        // act
        var result = await handler.Handle(request, default);

        // assert
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task GivenHandler_WhenAccountDomainException_ThenRollbackTransaction()
    {
        // arrange
        var request = _faker.Generate();
        _repositoryMock.FindByIdAsync(Arg.Is(request.Id), Arg.Is(_userContextMock.UserId), Arg.Any<CancellationToken>())
            .Returns(_accountFactory.Generate());
        _repositoryMock.SaveAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>()).Throws(
            new AccountDomainException(nameof(GivenHandler_WhenAccountDomainException_ThenRollbackTransaction)));

        var handler = new Handler(_userContextMock, _repositoryMock);

        // act
        Func<Task> action = () => handler.Handle(request, default);

        // assert
        using var scope = new AssertionScope();
        await action.Should().ThrowAsync<AccountDomainException>();
        _repositoryMock.UnitOfWork.Received(1).Dispose();
    }
}
