using System;
using DrifterApps.Holefeeder.Budgeting.Application.Models;
using MediatR;

namespace DrifterApps.Holefeeder.Budgeting.Application.Queries
{
    public class GetAccountQuery : IRequest<AccountViewModel>
    {
        public Guid UserId { get; }
        public Guid Id { get; }

        public GetAccountQuery(Guid userId, Guid id)
        {
            UserId = userId;
            Id = id;
        }
    }
}