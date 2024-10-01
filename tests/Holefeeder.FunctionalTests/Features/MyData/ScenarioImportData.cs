using System.Net;
using System.Text.Json;

using Holefeeder.Application;
using Holefeeder.Application.Features.MyData.Models;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;
using Holefeeder.Tests.Common;

using static Holefeeder.Application.Features.MyData.Commands.ImportData;
using static Holefeeder.Tests.Common.Builders.MyData.ImportDataRequestBuilder;
using static Holefeeder.Tests.Common.Builders.MyData.MyDataAccountDtoBuilder;
using static Holefeeder.Tests.Common.Builders.MyData.MyDataCashflowDtoBuilder;
using static Holefeeder.Tests.Common.Builders.MyData.MyDataCategoryDtoBuilder;
using static Holefeeder.Tests.Common.Builders.MyData.MyDataTransactionDtoBuilder;

namespace Holefeeder.FunctionalTests.Features.MyData;

[ComponentTest]
[Collection("Api collection")]
public class ScenarioImportData(ApiApplicationDriver applicationDriver, ITestOutputHelper testOutputHelper)
    : HolefeederScenario(applicationDriver, testOutputHelper)
{
    [Fact]
    public async Task WhenInvalidRequest()
    {
        var request = GivenAnImportDataRequest()
            .WithNoData()
            .Build();

        GivenUserIsAuthorized();

        await WhenUserImportsData(request);

        ShouldReceiveValidationProblemDetailsWithErrorMessage("One or more validation errors occurred.");
    }

    [Fact]
    public async Task WhenDataIsImported()
    {
        var accounts = GivenMyAccountData().BuildCollection(2);
        var categories = GivenMyCategoryData().BuildCollection(2);
        var cashflows = GivenMyCashflowData()
            .WithAccount(accounts.ElementAt(0))
            .WithCategory(categories.ElementAt(0))
            .BuildCollection(2);
        var transactions = GivenMyTransactionData()
            .WithAccount(accounts.ElementAt(0))
            .WithCategory(categories.ElementAt(0))
            .BuildCollection(2);

        var request = GivenAnImportDataRequest()
            .WithUpdateExisting()
            .WithAccounts(accounts.ToArray())
            .WithCategories(categories.ToArray())
            .WithCashflows(cashflows.ToArray())
            .WithTransactions(transactions.ToArray())
            .Build();

        GivenUserIsAuthorized();

        await WhenUserImportsData(request);

        ShouldExpectStatusCode(HttpStatusCode.Accepted);

        var location = ShouldGetTheRouteOfTheNewResourceInTheHeader();
        var id = ResourceIdFromLocation(location);

        var dto = await ThenWaitForCompletion(id);

        AssertAll(async () =>
        {
            dto.Should().NotBeNull("The import task never completed");
            dto!.Status.Should().NotBe(CommandStatus.Error, dto.Message);

            await AssertAccount(accounts.ElementAt(0));
            await AssertAccount(accounts.ElementAt(1));
            await AssertCategory(categories.ElementAt(0));
            await AssertCategory(categories.ElementAt(1));
            await AssertCashflow(cashflows.ElementAt(0));
            await AssertCashflow(cashflows.ElementAt(1));
            await AssertTransaction(transactions.ElementAt(0));
            await AssertTransaction(transactions.ElementAt(1));
        });

        async Task AssertAccount(MyDataAccountDto account)
        {
            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Accounts.FindAsync(account.Id);
            result.Should().NotBeNull();
            result!.Should().BeEquivalentTo(account);
        }

        async Task AssertCategory(MyDataCategoryDto category)
        {
            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Categories.FindAsync(category.Id);
            result.Should().NotBeNull();
            result!.Should().BeEquivalentTo(category);
        }

        async Task AssertCashflow(MyDataCashflowDto cashflow)
        {
            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Cashflows.FindAsync(cashflow.Id);
            result.Should().NotBeNull();
            result!.Should().BeEquivalentTo(cashflow);
        }

        async Task AssertTransaction(MyDataTransactionDto transaction)
        {
            await using var dbContext = DatabaseDriver.CreateDbContext();

            var result = await dbContext.Transactions.FindAsync(transaction.Id);
            result.Should().NotBeNull();
            result!.Should().BeEquivalentTo(transaction);
        }
    }

    private async Task WhenUserImportsData(Request request)
    {
        var json = JsonSerializer.Serialize(request, Globals.JsonSerializerOptions);
        await HttpClientDriver.SendRequestWithBodyAsync(ApiResources.ImportData, json);
    }

    private async Task<ImportDataStatusDto?> ThenWaitForCompletion(Guid importId)
    {
        var tries = 0;
        const int retryDelayInSeconds = 50;
        const int numberOfRetry = 10;

        while (tries < numberOfRetry)
        {
            await HttpClientDriver.SendRequestAsync(ApiResources.ImportDataStatus, importId);

            if (HttpClientDriver.ResponseStatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(TimeSpan.FromSeconds(retryDelayInSeconds));
                tries++;

                continue;
            }

            ShouldExpectStatusCode(HttpStatusCode.OK);

            var dto = HttpClientDriver.DeserializeContent<ImportDataStatusDto>();
            dto.Should().NotBeNull();
            if (dto!.Status == CommandStatus.Completed)
            {
                return dto;
            }

            if (dto.Status == CommandStatus.Error)
            {
                return dto;
            }

            await Task.Delay(TimeSpan.FromSeconds(retryDelayInSeconds));

            tries++;
        }

        return null;
    }
}
