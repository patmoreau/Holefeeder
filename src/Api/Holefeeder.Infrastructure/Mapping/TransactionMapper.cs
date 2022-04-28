﻿using System.Collections.Immutable;

using Holefeeder.Application.Features.MyData.Models;
using Holefeeder.Application.Models;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Infrastructure.Entities;

namespace Holefeeder.Infrastructure.Mapping;

internal class TransactionMapper
{
    private readonly AccountMapper _accountMapper;
    private readonly CategoryMapper _categoryMapper;
    private readonly TagsMapper _tagsMapper;

    public TransactionMapper(TagsMapper tagsMapper, AccountMapper accountMapper, CategoryMapper categoryMapper)
    {
        _tagsMapper = tagsMapper;
        _accountMapper = accountMapper;
        _categoryMapper = categoryMapper;
    }

    public Transaction? MapToModelOrNull(TransactionEntity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        var model = new Transaction
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            Amount = entity.Amount,
            CashflowDate = entity.CashflowDate,
            CashflowId = entity.CashflowId,
            CategoryId = entity.CategoryId,
            Date = entity.Date,
            Description = entity.Description,
            UserId = entity.UserId
        };

        return model.AddTags(_tagsMapper.Map(entity.Tags));
    }

    public TransactionInfoViewModel? MapToDtoOrNull(TransactionEntity? entity)
    {
        return entity is null ? null : MapToDto(entity);
    }

    public TransactionInfoViewModel MapToDto(TransactionEntity entity)
    {
        var dto = new TransactionInfoViewModel
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Date = entity.Date,
            Description = entity.Description,
            Tags = _tagsMapper.Map(entity.Tags).ToImmutableArray(),
            Account = _accountMapper.MapToAccountInfoViewModel(entity.Account),
            Category = _categoryMapper.MapToCategoryInfoViewModel(entity.Category)
        };

        return dto;
    }

    public IEnumerable<TransactionInfoViewModel> MapToDto(IEnumerable<TransactionEntity> entities)
    {
        return entities.Select(MapToDto);
    }

    public TransactionEntity MapToEntity(Transaction model)
    {
        var entity = new TransactionEntity
        {
            Id = model.Id,
            AccountId = model.AccountId,
            Amount = model.Amount,
            CashflowDate = model.CashflowDate,
            CashflowId = model.CashflowId,
            CategoryId = model.CategoryId,
            Date = model.Date,
            Description = model.Description,
            Tags = _tagsMapper.Map(model.Tags),
            UserId = model.UserId
        };

        return entity;
    }

    public MyDataTransactionDto MapToMyDataTransactionDto(TransactionEntity entity)
    {
        var dto = new MyDataTransactionDto
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Date = entity.Date,
            Description = entity.Description,
            Tags = _tagsMapper.Map(entity.Tags).ToArray(),
            AccountId = entity.AccountId,
            CategoryId = entity.CategoryId,
            CashflowId = entity.CashflowId,
            CashflowDate = entity.CashflowDate
        };

        return dto;
    }
}