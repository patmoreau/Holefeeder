﻿using System.Threading;
using System.Threading.Tasks;

using AutoBogus;

using Bogus;

using FluentAssertions;

using FluentValidation.TestHelper;

using Holefeeder.Application.Features.Transactions;
using Holefeeder.Application.Models;
using Holefeeder.Application.SeedWork;
using Holefeeder.Tests.Common.Factories;

using Microsoft.AspNetCore.Http;

using NSubstitute;

using Xunit;

using static Holefeeder.Application.Features.Transactions.Queries.GetTransactions;

namespace Holefeeder.UnitTests.Application.Features.Transactions.Queries;

public class GetTransactionsTests
{
    private readonly Faker<Request> _faker;
    private readonly ITransactionQueriesRepository _repositoryMock = Substitute.For<ITransactionQueriesRepository>();

    private readonly IUserContext _userContextMock = MockHelper.CreateUserContext();

    private readonly TransactionInfoViewModelFactory _viewModelFactory = new();

    public GetTransactionsTests()
    {
        _faker = new AutoFaker<Request>()
            .RuleFor(fake => fake.Offset, fake => fake.Random.Number())
            .RuleFor(fake => fake.Limit, fake => fake.Random.Int(1));
    }

    [Fact]
    public async Task GivenRequest_WhenBindingFromHttpContext_ThenReturnRequest()
    {
        // arrange
        var httpContext = new DefaultHttpContext
        {
            Request = {QueryString = new QueryString("?offset=10&limit=100&sort=data&filter=code:eq:settings")}
        };

        // act
        var result = await Request.BindAsync(httpContext, null!);

        // assert
        result.Should().BeEquivalentTo(new Request(10, 100, new[] {"data"}, new[] {"code:eq:settings"}));
    }

    [Fact]
    public void GivenValidator_WhenOffsetIsInvalid_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Offset, -1).Generate();

        var validator = new Validator();

        // act
        var result = validator.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Offset);
    }

    [Fact]
    public void GivenValidator_WhenLimitIsInvalid_ThenError()
    {
        // arrange
        var request = _faker.RuleFor(x => x.Limit, 0).Generate();

        var validator = new Validator();

        // act
        var result = validator.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(r => r.Limit);
    }

    [Fact]
    public async Task GivenHandler_WhenIdFound_ThenReturnResult()
    {
        // arrange
        var request = _faker.Generate();
        var count = new Faker().Random.Number(100);
        var models = _viewModelFactory.Generate(count);

        _repositoryMock.FindAsync(Arg.Is(_userContextMock.UserId), Arg.Any<QueryParams>(), Arg.Any<CancellationToken>())
            .Returns((count, models));

        var handler = new Handler(_userContextMock, _repositoryMock);

        // act
        var result = await handler.Handle(request, default);

        // assert
        result.Should().Be(new QueryResult<TransactionInfoViewModel>(count, models));
    }
}
