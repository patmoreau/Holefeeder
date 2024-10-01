using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.ValueObjects;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Commands;

public class ModifyCashflow : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/cashflows/modify",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.NoContent()
                    };
                })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(ModifyCashflow))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request : IRequest<Result>, IUnitOfWorkRequest
    {
        public required CashflowId Id { get; init; }

        public required Money Amount { get; init; }

        public DateOnly EffectiveDate { get; init; }

        public string Description { get; init; } = null!;

        public string[] Tags { get; init; } = [];
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Id).NotNull().NotEqual(CashflowId.Empty);
            RuleFor(command => command.EffectiveDate).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Result>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken) =>
            GetExistingCashflow(request, cancellationToken)
                .OnSuccess(ModifyCashflow(request));

        private async Task<Result<Cashflow>> GetExistingCashflow(Request request, CancellationToken cancellationToken)
        {
            var cashflow = await context.Cashflows.SingleOrDefaultAsync(
                x => x.Id == request.Id && x.UserId == userContext.Id,
                cancellationToken);
            return cashflow is null
                ? Result<Cashflow>.Failure(CashflowErrors.NotFound(request.Id))
                : Result<Cashflow>.Success(cashflow);
        }

        private Func<Cashflow, Task<Result>> ModifyCashflow(Request request) =>
            cashflow => Task.FromResult(
                cashflow.Modify(amount: request.Amount, effectiveDate: request.EffectiveDate,
                        description: request.Description)
                    .OnSuccess(SetTags(request))
                    .OnSuccess(SaveCashflow()));

        private static Func<Cashflow, Result<Cashflow>> SetTags(Request request) => cashflow => cashflow.SetTags(request.Tags);

        private Func<Cashflow, Result> SaveCashflow() =>
            cashflow =>
            {
                context.Update(cashflow);
                return Result.Success();
            };
    }
}
