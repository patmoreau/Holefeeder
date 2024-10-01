using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Models;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Transactions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Queries;

public class GetCashflow : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/cashflows/{id}",
                async (CashflowId id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new Request(id), cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.Ok(result.Value)
                    };
                })
            .Produces<CashflowInfoViewModel>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(nameof(Transactions))
            .WithName(nameof(GetCashflow))
            .RequireAuthorization(Policies.ReadUser);

    internal record Request(CashflowId Id) : IRequest<Result<CashflowInfoViewModel>>;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Id).NotEqual(CashflowId.Empty);
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<CashflowInfoViewModel>>
    {
        public async Task<Result<CashflowInfoViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            var result = await context.Cashflows
                .Include(x => x.Account).Include(x => x.Category)
                .SingleOrDefaultAsync(x => x.Id == request.Id && x.UserId == userContext.Id, cancellationToken);
            return result is null
                ? Result<CashflowInfoViewModel>.Failure(CashflowErrors.NotFound(request.Id))
                : Result<CashflowInfoViewModel>.Success(CashflowMapper.MapToDto(result));
        }
    }
}
