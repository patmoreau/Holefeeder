using System.Text.Json;

using DrifterApps.Seeds.Testing.Drivers;
using DrifterApps.Seeds.Testing.Infrastructure;
using DrifterApps.Seeds.Testing.Scenarios;
using DrifterApps.Seeds.Testing.StepDefinitions;

using Holefeeder.Application.Features.Transactions.Commands;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.FunctionalTests.Drivers;
using Holefeeder.FunctionalTests.Infrastructure;

using MediatR;

using static Holefeeder.Tests.Common.Builders.Transactions.CancelCashflowRequestBuilder;
using static Holefeeder.Tests.Common.Builders.Transactions.CashflowBuilder;

namespace Holefeeder.FunctionalTests.StepDefinitions;

public class CashflowStepDefinition(IHttpClientDriver httpClientDriver, BudgetingDatabaseDriver budgetingDatabaseDriver)
    : StepDefinition(httpClientDriver)
{
    private const string ContextCashflowRequest = $"{nameof(CashflowStepDefinition)}_{nameof(ContextCashflowRequest)}";
    public const string ContextExistingCashflow = $"{nameof(CashflowStepDefinition)}_{nameof(ContextExistingCashflow)}";

#pragma warning disable CA1822
    public void AnInvalidCancelRequest(IStepRunner runner)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(runner);

        runner.Execute("an invalid cashflow request", () =>
        {
            var request = GivenAnInvalidCancelCashflowRequest().Build();
            runner.SetContextData(ContextCashflowRequest, request);
        });
    }

    internal void RequestIsSent(IStepRunner runner)
    {
        runner.Execute("the cashflow request is sent", async () =>
        {
            var request = runner.GetContextData<IBaseRequest>(ContextCashflowRequest);

            var json = JsonSerializer.Serialize(request);

            ApiResource apiResource = default!;
            if (request is CancelCashflow.Request)
            {
                apiResource = ApiResources.CancelCashflow;
            }

            await HttpClientDriver.SendPostRequestAsync(apiResource, json);
        });
    }

    internal void Exists(IStepRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);

        runner.Execute("a user has an active cashflow", () =>
        {
            var account = runner.GetContextData<Account>(AccountStepDefinition.ContextExistingAccount);
            account.Should().NotBeNull();

            var category = runner.GetContextData<Category>(CategoryStepDefinition.ContextExistingCategory);
            category.Should().NotBeNull();

            var cashflow = GivenAnActiveCashflow()
                .ForAccount(account)
                .ForCategory(category)
                .ForUser(UserStepDefinition.HolefeederUserId)
                .SavedInDbAsync(budgetingDatabaseDriver);

            runner.SetContextData(ContextExistingCashflow, cashflow);
        });
    }

}
