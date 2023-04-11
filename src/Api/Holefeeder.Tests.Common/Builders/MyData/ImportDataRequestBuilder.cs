using Holefeeder.Application.Features.MyData.Models;
using Holefeeder.Tests.Common.SeedWork;
using static Holefeeder.Application.Features.MyData.Commands.ImportData;

namespace Holefeeder.Tests.Common.Builders.MyData;

internal class ImportDataRequestBuilder : RootBuilder<Request>
{
    private readonly Faker<Request.Dto> _dtoFaker = new AutoFaker<Request.Dto>();

    public override Request Build()
    {
        Request.Dto? data = _dtoFaker.Generate();

        Faker.RuleFor(f => f.Data, data);

        return base.Build();
    }

    public static ImportDataRequestBuilder GivenAnImportDataRequest() => new();

    public ImportDataRequestBuilder WithNoData()
    {
        _dtoFaker.RuleFor(f => f.Accounts, Array.Empty<MyDataAccountDto>());
        _dtoFaker.RuleFor(f => f.Cashflows, Array.Empty<MyDataCashflowDto>());
        _dtoFaker.RuleFor(f => f.Categories, Array.Empty<MyDataCategoryDto>());
        _dtoFaker.RuleFor(f => f.Transactions, Array.Empty<MyDataTransactionDto>());
        return this;
    }

    public ImportDataRequestBuilder WithUpdateExisting()
    {
        Faker.RuleFor(f => f.UpdateExisting, true);
        return this;
    }

    public ImportDataRequestBuilder WithAccounts(params MyDataAccountDto[] accounts)
    {
        _dtoFaker.RuleFor(f => f.Accounts, accounts);
        return this;
    }

    public ImportDataRequestBuilder WithCashflows(params MyDataCashflowDto[] cashflows)
    {
        _dtoFaker.RuleFor(f => f.Cashflows, cashflows);
        return this;
    }

    public ImportDataRequestBuilder WithCategories(params MyDataCategoryDto[] categories)
    {
        _dtoFaker.RuleFor(f => f.Categories, categories);
        return this;
    }

    public ImportDataRequestBuilder WithTransactions(params MyDataTransactionDto[] transactions)
    {
        _dtoFaker.RuleFor(f => f.Transactions, transactions);
        return this;
    }
}
