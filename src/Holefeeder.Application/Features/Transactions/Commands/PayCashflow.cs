using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Features.Transactions.Queries;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.ValueObjects;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Commands;

public class PayCashflow : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/transactions/pay-cashflow",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.CreatedAtRoute(nameof(GetTransaction), new { Id = (Guid)result.Value },
                            new { Id = (Guid)result.Value })
                    };
                })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(PayCashflow))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request : IRequest<Result<TransactionId>>, IUnitOfWorkRequest
    {
        public DateOnly Date { get; init; }

        public Money Amount { get; init; }

        public required CashflowId CashflowId { get; init; }

        public DateOnly CashflowDate { get; init; }
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Date).NotEmpty();
            RuleFor(command => command.CashflowId).NotEqual(CashflowId.Empty);
            RuleFor(command => command.CashflowDate).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Result<TransactionId>>
    {
        public Task<Result<TransactionId>> Handle(Request request, CancellationToken cancellationToken) =>
            GetExistingCashflowAsync(request, cancellationToken)
                .OnSuccess(CreateCashflowTransactionAsync(request, cancellationToken));

        private async Task<Result<Cashflow>> GetExistingCashflowAsync(Request request, CancellationToken cancellationToken)
        {
            var cashflow = await context.Cashflows.SingleOrDefaultAsync(
                x => x.Id == request.CashflowId && x.UserId == userContext.Id,
                cancellationToken);
            return cashflow is null
                ? Result<Cashflow>.Failure(CashflowErrors.NotFound(request.CashflowId))
                : Result<Cashflow>.Success(cashflow);
        }

        private Func<Cashflow, Task<Result<TransactionId>>> CreateCashflowTransactionAsync(Request request,
            CancellationToken cancellationToken) =>
            cashflow => Task.FromResult(
                    Transaction.Create(request.Date, request.Amount, cashflow.Description,
                            cashflow.AccountId, cashflow.CategoryId, userContext.Id)
                        .OnSuccess(transaction => transaction.ApplyCashflow(cashflow.Id, request.CashflowDate))
                        .OnSuccess(transaction => transaction.SetTags(cashflow.Tags.ToArray())))
                .OnSuccess(SaveTransactionAsync(cancellationToken));

        private Func<Transaction, Task<Result<TransactionId>>> SaveTransactionAsync(CancellationToken cancellationToken) =>
            async transaction =>
            {
                await context.Transactions.AddAsync(transaction, cancellationToken);

                return Result<TransactionId>.Success(transaction.Id);
            };
    }
}
