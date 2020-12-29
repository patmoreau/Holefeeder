using System;
using System.Threading;
using System.Threading.Tasks;
using DrifterApps.Holefeeder.Budgeting.Application.Contracts;
using DrifterApps.Holefeeder.Budgeting.Application.Models;
using DrifterApps.Holefeeder.Budgeting.Domain.Enumerations;
using DrifterApps.Holefeeder.Framework.SeedWork;
using MediatR;

namespace DrifterApps.Holefeeder.Budgeting.Application.Queries
{
    public class GetAccountHandler : IRequestHandler<GetAccountQuery, AccountViewModel>
    {
        private readonly IAccountQueriesRepository _repository;

        public GetAccountHandler(IAccountQueriesRepository repository)
        {
            _repository = repository;
        }

        public async Task<AccountViewModel> Handle(GetAccountQuery query,
            CancellationToken cancellationToken = default)
        {
            query.ThrowIfNull(nameof(query));

            return await Task.FromResult(new AccountViewModel(
                query.Id,
                AccountType.Checking,
                "Test Account",
                99, 123.45m, DateTime.Today, "This is a test account", true));
        }
    }
}