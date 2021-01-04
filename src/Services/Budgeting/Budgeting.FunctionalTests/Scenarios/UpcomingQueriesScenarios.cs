﻿using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using DrifterApps.Holefeeder.Budgeting.API;
using DrifterApps.Holefeeder.Budgeting.Application.Converters;
using DrifterApps.Holefeeder.Budgeting.Application.Models;
using DrifterApps.Holefeeder.Budgeting.Domain.Enumerations;
using DrifterApps.Holefeeder.Budgeting.Domain.Extensions;
using DrifterApps.Holefeeder.Framework.SeedWork.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace DrifterApps.Holefeeder.Budgeting.FunctionalTests.Scenarios
{
    public class UpcomingQueriesScenarios : IClassFixture<BudgetingWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public UpcomingQueriesScenarios(BudgetingWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GivenGetUpcoming_WhenNoFilterApplied_ThenReturnAllItems()
        {
            // Arrange
            var from = (new DateTime(2020, 1, 1));
            var client = _factory.CreateClient();
            string request = $"/api/v2/cashflows/get-upcoming?from={from.ToPersistent()}&to={from.AddDays(14).ToPersistent()}";

            // Act
            var response = await client.GetAsync(request);

            // Assert
            response.StatusCodeShouldBeSuccess();

            var jsonOptions = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
            jsonOptions.Converters.Add(new CategoryTypeConverter());
            var result = await response.Content.ReadFromJsonAsync<UpcomingViewModel[]>(jsonOptions);

            result.Single().Should().BeEquivalentTo(
                new UpcomingViewModel(BudgetingContextSeed.CashflowGuid1,
                new DateTime(2020, 1, 2), 111, "Cashflow1",
                new CategoryInfoViewModel(BudgetingContextSeed.CategoryGuid1, "Category1", CategoryType.Expense, String.Empty),
                new AccountInfoViewModel(BudgetingContextSeed.AccountGuid1, "Account1", String.Empty),
                null), options => options.Excluding(x => x.Account.MongoId));
        }
    }
}
