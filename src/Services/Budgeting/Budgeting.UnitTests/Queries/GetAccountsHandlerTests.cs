﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DrifterApps.Holefeeder.Budgeting.Application.Contracts;
using DrifterApps.Holefeeder.Budgeting.Application.Models;
using DrifterApps.Holefeeder.Budgeting.Application.Queries;
using DrifterApps.Holefeeder.Framework.SeedWork.Application;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DrifterApps.Holefeeder.Budgeting.UnitTests.Queries
{
    public class GetAccountsHandlerTests
    {
        [Fact]
        public async void GivenHandle_WhenRequestIsNull_ThenThrowArgumentNullException()
        {
            var handler = new GetAccountsHandler(Substitute.For<IAccountQueriesRepository>());

            Func<Task> action = async () => await handler.Handle(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async void GivenHandle_WhenRequestIsValid_ThenReturnData()
        {
            var repository = Substitute.For<IAccountQueriesRepository>();
            repository.GetAccountsAsync(Arg.Any<Guid>(), Arg.Any<QueryParams>(), CancellationToken.None)
                .Returns(new List<AccountViewModel>());
            
            var handler = new GetAccountsHandler(repository);
            

            Func<Task> action = async () =>
                await handler.Handle(new GetAccountsQuery(Guid.NewGuid(), null, null, null, null));

            await action.Should().NotThrowAsync();
        }
    }
}
