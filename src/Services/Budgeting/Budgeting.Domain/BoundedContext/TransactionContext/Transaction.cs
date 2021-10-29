﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using DrifterApps.Holefeeder.Budgeting.Domain.Exceptions;
using DrifterApps.Holefeeder.Framework.SeedWork.Domain;

namespace DrifterApps.Holefeeder.Budgeting.Domain.BoundedContext.TransactionContext
{
    public record Transaction : IAggregateRoot
    {
        private readonly Guid _id;
        private readonly DateTime _date;
        private readonly decimal _amount;
        private readonly Guid _userId;

        public Guid Id
        {
            get => _id;
            init
            {
                if (value.Equals(default))
                {
                    throw new HolefeederDomainException($"{nameof(Id)} is required");
                }

                _id = value;
            }
        }

        public DateTime Date
        {
            get => _date;
            init
            {
                if (value.Equals(default))
                {
                    throw new HolefeederDomainException($"{nameof(Date)} is required");
                }

                _date = value;
            }
        }

        public decimal Amount
        {
            get => _amount;
            init
            {
                if (value < 0m)
                {
                    throw new HolefeederDomainException($"{nameof(Amount)} cannot be negative");
                }

                _amount = value;
            }
        }

        public string Description { get; init; }

        public Guid AccountId { get; init; }

        public Guid CategoryId { get; init; }

        public Guid? CashflowId { get; private init; }

        public DateTime? CashflowDate { get; private init; }

        public IReadOnlyCollection<string> Tags { get; private init; }

        public Guid UserId
        {
            get => _userId;
            init
            {
                if (value.Equals(default))
                {
                    throw new HolefeederDomainException($"{nameof(UserId)} is required");
                }

                _userId = value;
            }
        }

        private Transaction()
        {
            Tags = ImmutableList<string>.Empty;
        }

        public static Transaction Create(DateTime date, decimal amount, string description, Guid categoryId,
            Guid accountId, Guid userId)
            => new()
            {
                Id = Guid.NewGuid(),
                Date = date,
                Amount = amount,
                Description = description,
                CategoryId = categoryId,
                AccountId = accountId,
                UserId = userId
            };

        public static Transaction Create(DateTime date, decimal amount, string description, Guid categoryId,
            Guid accountId, Guid cashflowId, DateTime cashflowDate, Guid userId)
        {
            if (cashflowId.Equals(default))
            {
                throw new HolefeederDomainException($"{nameof(CashflowId)} is required");
            }

            if (cashflowDate.Equals(default))
            {
                throw new HolefeederDomainException($"{nameof(CashflowDate)} is required");
            }

            return new Transaction
            {
                Id = Guid.NewGuid(),
                Date = date,
                Amount = amount,
                Description = description,
                CategoryId = categoryId,
                AccountId = accountId,
                UserId = userId
            };
        }

        public Transaction AddTags(params string[] tags)
        {
            var newTags = tags.Where(t => !string.IsNullOrWhiteSpace(t) &&
                                          !Tags.Contains(t,
                                              StringComparer.Create(CultureInfo.InvariantCulture,
                                                  CompareOptions.IgnoreCase)))
                .ToList();
            
            if (!newTags.Any())
            {
                return this;
            }

            return this with { Tags = newTags.ToImmutableList() };
        }
    }
}
