using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Features.Transactions.Queries;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Enumerations;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.ValueObjects;

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
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.CreatedAtRoute(nameof(GetTransaction), new { Id = (Guid)result.Value },
                            new { Id = (Guid)result.Value })
                    };
                })
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(MakePurchase))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request : IRequest<Result<TransactionId>>, IUnitOfWorkRequest
    {
        public DateOnly Date { get; init; }

        public Money Amount { get; init; }

        public string Description { get; init; } = null!;

        public AccountId AccountId { get; init; } = AccountId.Empty;

        public CategoryId CategoryId { get; init; } = CategoryId.Empty;

        public string[] Tags { get; init; } = null!;

        public CashflowRequest? Cashflow { get; init; }

        internal record CashflowRequest(
            DateOnly EffectiveDate,
            DateIntervalType IntervalType,
            int Frequency,
            int Recurrence);
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.AccountId).NotNull().NotEqual(AccountId.Empty);
            RuleFor(command => command.CategoryId).NotNull().NotEqual(CategoryId.Empty);
            RuleFor(command => command.Date).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<TransactionId>>
    {
        public async Task<Result<TransactionId>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!await context.Accounts.AnyAsync(x => x.Id == request.AccountId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                Result<TransactionId>.Failure(TransactionErrors.AccountNotFound(request.AccountId));
            }

            if (!await context.Categories.AnyAsync(x => x.Id == request.CategoryId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                return Result<TransactionId>.Failure(TransactionErrors.CategoryNotFound(request.CategoryId));
            }

            var cashflowId = await HandleCashflow(request, cancellationToken);
            if (cashflowId.IsFailure)
            {
                return Result<TransactionId>.Failure(cashflowId.Error);
            }

            var transaction = Transaction.Create(request.Date, request.Amount, request.Description,
                request.AccountId, request.CategoryId, userContext.Id);
            if (transaction.IsFailure)
            {
                return Result<TransactionId>.Failure(transaction.Error);
            }

            if (cashflowId.Value is not null && cashflowId.Value != CashflowId.Empty)
            {
                transaction = transaction.Value.ApplyCashflow(cashflowId.Value, request.Date);
                if (transaction.IsFailure)
                {
                    return Result<TransactionId>.Failure(transaction.Error);
                }
            }

            transaction = transaction.Value.SetTags(request.Tags);
            if (transaction.IsFailure)
            {
                return Result<TransactionId>.Failure(transaction.Error);
            }

            await context.Transactions.AddAsync(transaction.Value, cancellationToken);

            return Result<TransactionId>.Success(transaction.Value.Id);
        }

        private async Task<Result<CashflowId?>> HandleCashflow(Request request, CancellationToken cancellationToken)
        {
            if (request.Cashflow is null)
            {
                return Result<CashflowId?>.Success(CashflowId.Empty);
            }

            var cashflowRequest = request.Cashflow;
            var result = Cashflow.Create(cashflowRequest.EffectiveDate, cashflowRequest.IntervalType,
                cashflowRequest.Frequency, cashflowRequest.Recurrence, request.Amount, request.Description,
                request.CategoryId, request.AccountId, userContext.Id);

            if (result.IsFailure)
            {
                return Result<CashflowId?>.Failure(result.Error);
            }

            var cashflow = result.Value.SetTags(request.Tags);
            if (cashflow.IsFailure)
            {
                return Result<CashflowId?>.Failure(cashflow.Error);
            }

            await context.Cashflows.AddAsync(cashflow.Value, cancellationToken);

            return Result<CashflowId?>.Success(cashflow.Value.Id);
        }
    }
}
