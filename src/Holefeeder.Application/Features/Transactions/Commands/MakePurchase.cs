using DrifterApps.Seeds.Application.Mediatr;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Features.Transactions.Queries;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Enumerations;
using Holefeeder.Domain.Features.Transactions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Commands;

public class MakePurchase : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/transactions/make-purchase",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return Results.CreatedAtRoute(nameof(GetTransaction), new { Id = result }, new { Id = result });
                })
            .Produces<Unit>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(MakePurchase))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request : IRequest<Guid>, IUnitOfWorkRequest
    {
        public DateOnly Date { get; init; }

        public decimal Amount { get; init; }

        public string Description { get; init; } = null!;

        public Guid AccountId { get; init; }

        public Guid CategoryId { get; init; }

        public string[] Tags { get; init; } = null!;

        public CashflowRequest? Cashflow { get; init; }

        internal record CashflowRequest(DateOnly EffectiveDate, DateIntervalType IntervalType, int Frequency,
            int Recurrence);
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.AccountId).NotNull().NotEmpty();
            RuleFor(command => command.CategoryId).NotNull().NotEmpty();
            RuleFor(command => command.Date).NotEmpty();
            RuleFor(command => command.Amount).GreaterThanOrEqualTo(0);
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Guid>
    {
        public async Task<Guid> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!await context.Accounts.AnyAsync(x => x.Id == request.AccountId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                throw new TransactionDomainException($"Account '{request.AccountId}' does not exists.");
            }

            if (!await context.Categories.AnyAsync(x => x.Id == request.CategoryId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                throw new TransactionDomainException($"Category '{request.CategoryId}' does not exists.");
            }

            var cashflowId = await HandleCashflow(request, cancellationToken);

            var transaction = Transaction.Create(request.Date, request.Amount, request.Description,
                request.AccountId, request.CategoryId, userContext.Id);

            if (cashflowId is not null)
            {
                transaction = transaction.ApplyCashflow(cashflowId.Value, request.Date);
            }

            transaction = transaction.SetTags(request.Tags);

            await context.Transactions.AddAsync(transaction, cancellationToken);

            return transaction.Id;
        }

        private async Task<Guid?> HandleCashflow(Request request, CancellationToken cancellationToken)
        {
            if (request.Cashflow is null)
            {
                return null;
            }

            var cashflowRequest = request.Cashflow;
            var cashflow = Cashflow.Create(cashflowRequest.EffectiveDate, cashflowRequest.IntervalType,
                cashflowRequest.Frequency, cashflowRequest.Recurrence, request.Amount, request.Description,
                request.CategoryId, request.AccountId, userContext.Id);

            cashflow = cashflow.SetTags(request.Tags);

            await context.Cashflows.AddAsync(cashflow, cancellationToken);

            return cashflow.Id;
        }
    }
}
