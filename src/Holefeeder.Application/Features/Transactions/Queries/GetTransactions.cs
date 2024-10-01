using System.Reflection;

using DrifterApps.Seeds.Application;
using DrifterApps.Seeds.Application.Extensions;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Models;
using Holefeeder.Application.UserContext;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Queries;

public class GetTransactions : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/transactions",
                async (Request request, IMediator mediator, HttpContext ctx, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    switch (result)
                    {
                        case { IsFailure: true }:
                            return result.Error.ToProblem();
                        default:
                            ctx.Response.Headers.Append("X-Total-Count", $"{result.Value.Total}");
                            return Results.Ok(result.Value.Items);
                    }
                })
            .Produces<IEnumerable<TransactionInfoViewModel>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(nameof(IRequestQuery))
            .WithTags(nameof(Transactions))
            .WithName(nameof(GetTransactions))
            .RequireAuthorization(Policies.ReadUser);

    internal record Request(int Offset, int Limit, string[] Sort, string[] Filter)
        : IRequest<Result<QueryResult<TransactionInfoViewModel>>>, IRequestQuery
    {
        public static ValueTask<Request?> BindAsync(HttpContext context, ParameterInfo parameter) =>
            context.ToQueryRequest((offset, limit, sort, filter) => new Request(offset, limit, sort, filter));
    }

    internal class Validator : QueryValidatorRoot<Request>;

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<QueryResult<TransactionInfoViewModel>>>
    {
        public async Task<Result<QueryResult<TransactionInfoViewModel>>> Handle(Request request,
            CancellationToken cancellationToken)
        {
            var queryParams = QueryParams.Create(request);
            if (queryParams.IsFailure)
            {
                return Result<QueryResult<TransactionInfoViewModel>>.Failure(queryParams.Error);
            }

            var total = await context.Transactions.CountAsync(e => e.UserId == userContext.Id, cancellationToken);
            var items = await context.Transactions
                .Include(e => e.Account)
                .Include(e => e.Category)
                .Where(e => e.UserId == userContext.Id)
                .Query(queryParams.Value)
                .Select(e => TransactionMapper.MapToDto(e))
                .ToListAsync(cancellationToken);

            return Result<QueryResult<TransactionInfoViewModel>>.Success(
                new QueryResult<TransactionInfoViewModel>(total, items));
        }
    }
}
