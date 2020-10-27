﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DrifterApps.Holefeeder.Application.Transactions.Contracts;
using DrifterApps.Holefeeder.Application.Transactions.Models;
using DrifterApps.Holefeeder.Framework.SeedWork;
using MediatR;

namespace DrifterApps.Holefeeder.Application.Transactions.Queries
{
    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, CategoryViewModel[]>
    {
        private readonly ICategoryQueries _categoryQueries;

        public GetCategoriesHandler(ICategoryQueries categoryQueries)
        {
            _categoryQueries = categoryQueries;
        }

        public async Task<CategoryViewModel[]> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(request));

            return (await _categoryQueries.GetCategoriesAsync(cancellationToken)).ToArray();
        }
    }
}
