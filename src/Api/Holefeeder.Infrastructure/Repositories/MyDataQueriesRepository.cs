﻿using Holefeeder.Application.Features.MyData;
using Holefeeder.Application.Features.MyData.Models;
using Holefeeder.Infrastructure.Context;
using Holefeeder.Infrastructure.Entities;
using Holefeeder.Infrastructure.Extensions;
using Holefeeder.Infrastructure.Mapping;

namespace Holefeeder.Infrastructure.Repositories;

internal class MyDataQueriesRepository : IMyDataQueriesRepository
{
    private readonly AccountMapper _accountMapper;
    // private readonly CashflowMapper _cashflowMapper;
    // private readonly CategoryMapper _categoryMapper;
    private readonly IHolefeederContext _context;
    // private readonly TransactionMapper _transactionMapper;

    public MyDataQueriesRepository(IHolefeederContext context, AccountMapper accountMapper
        // CategoryMapper categoryMapper, CashflowMapper cashflowMapper, TransactionMapper transactionMapper
        )
    {
        _context = context;
        _accountMapper = accountMapper;
        // _categoryMapper = categoryMapper;
        // _cashflowMapper = cashflowMapper;
        // _transactionMapper = transactionMapper;
    }

    public async Task<IEnumerable<MyDataAccountDto>> ExportAccountsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Connection;

        var schema = await connection
            .FindAsync<AccountEntity>(new {UserId = userId})
            .ConfigureAwait(false);

        return schema.Select(_accountMapper.MapToMyDataAccountDto);
    }

    // public async Task<IEnumerable<MyDataCategoryDto>> ExportCategoriesAsync(Guid userId,
    //     CancellationToken cancellationToken = default)
    // {
    //     var connection = _context.Connection;
    //
    //     var schema = await connection
    //         .FindAsync<CategoryEntity>(new {UserId = userId})
    //         .ConfigureAwait(false);
    //
    //     return schema.Select(_categoryMapper.MapToMyDataCategoryDto);
    // }
    //
    // public async Task<IEnumerable<MyDataCashflowDto>> ExportCashflowsAsync(Guid userId,
    //     CancellationToken cancellationToken = default)
    // {
    //     var connection = _context.Connection;
    //
    //     var schema = await connection
    //         .FindAsync<CashflowEntity>(new {UserId = userId})
    //         .ConfigureAwait(false);
    //
    //     return schema.Select(_cashflowMapper.MapToMyDataCashflowDto);
    // }
    //
    // public async Task<IEnumerable<MyDataTransactionDto>> ExportTransactionsAsync(Guid userId,
    //     CancellationToken cancellationToken = default)
    // {
    //     var connection = _context.Connection;
    //
    //     var schema = await connection
    //         .FindAsync<TransactionEntity>(new {UserId = userId})
    //         .ConfigureAwait(false);
    //
    //     return schema.Select(_transactionMapper.MapToMyDataTransactionDto);
    // }
}