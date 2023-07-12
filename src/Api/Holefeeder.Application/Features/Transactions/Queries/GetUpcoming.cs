﻿using System.Collections.Immutable;
using System.Reflection;
using DrifterApps.Seeds.Application;
using Holefeeder.Application.Context;
using Holefeeder.Application.Features.Accounts.Queries;
using Holefeeder.Application.Models;
using Holefeeder.Domain.Features.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Queries;

public class GetUpcoming : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/cashflows/get-upcoming",
                async (Request request, IMediator mediator, HttpContext ctx, CancellationToken cancellationToken) =>
                {
                    (int total, IEnumerable<UpcomingViewModel> viewModels) =
                        await mediator.Send(request, cancellationToken);
                    ctx.Response.Headers.Add("X-Total-Count", $"{total}");
                    return Results.Ok(viewModels);
                })
            .Produces<IEnumerable<UpcomingViewModel>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(GetUpcoming))
            .RequireAuthorization();

    internal record Request(DateOnly From, DateOnly To) : IRequest<QueryResult<UpcomingViewModel>>
    {
        public static ValueTask<Request?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            const string fromKey = "from";
            const string toKey = "to";

            bool hasFrom = DateOnly.TryParse(context.Request.Query[fromKey], out DateOnly from);
            bool hasTo = DateOnly.TryParse(context.Request.Query[toKey], out DateOnly to);

            Request result = new(hasFrom ? from : DateOnly.MinValue, hasTo ? to : DateOnly.MaxValue);

            return ValueTask.FromResult<Request?>(result);
        }
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.From).NotEmpty();
            RuleFor(command => command.To)
                .NotEmpty()
                .GreaterThanOrEqualTo(command => command.From)
                .WithMessage($"{nameof(Request.To)} must be greater or equal to {nameof(Request.To)}.");
        }
    }

    internal class Handler : IRequestHandler<Request, QueryResult<UpcomingViewModel>>
    {
        private readonly BudgetingContext _context;
        private readonly IUserContext _userContext;

        public Handler(IUserContext userContext, BudgetingContext context)
        {
            _userContext = userContext;
            _context = context;
        }

        public async Task<QueryResult<UpcomingViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<Cashflow> cashflows = await _context.Cashflows
                .Where(c => c.UserId == _userContext.Id)
                .Include(c => c.Account)
                .Include(c => c.Category)
                .Include(c => c.Transactions)
                .ToListAsync(cancellationToken);

            List<UpcomingViewModel> results = cashflows
                .SelectMany(x => x.GetUpcoming(request.To)
                    .Select(d => new UpcomingViewModel
                    {
                        Id = x.Id,
                        Date = d,
                        Amount = x.Amount,
                        Description = x.Description,
                        Tags = x.Tags.ToImmutableArray(),
                        Category = new CategoryInfoViewModel(x.Category!.Id, x.Category.Name, x.Category.Type,
                            x.Category.Color),
                        Account = new AccountInfoViewModel(x.Account!.Id, x.Account.Name)
                    }))
                .Where(x => x.Date <= request.To)
                .OrderBy(x => x.Date)
                .ToList();

            return new QueryResult<UpcomingViewModel>(results.Count, results);
        }
    }
}
