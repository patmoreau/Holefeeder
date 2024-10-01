using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.Features.Categories;
using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.ValueObjects;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Commands;

public class ModifyTransaction : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/transactions/modify",
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
            .WithName(nameof(ModifyTransaction))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request : IRequest<Result>, IUnitOfWorkRequest
    {
        public required TransactionId Id { get; init; }

        public DateOnly Date { get; init; }

        public Money Amount { get; init; }

        public string Description { get; init; } = null!;

        public required AccountId AccountId { get; init; }

        public required CategoryId CategoryId { get; init; }

        public string[] Tags { get; init; } = null!;
    }

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Id).NotNull().NotEqual(TransactionId.Empty);
            RuleFor(command => command.AccountId).NotNull().NotEqual(AccountId.Empty);
            RuleFor(command => command.CategoryId).NotNull().NotEqual(CategoryId.Empty);
            RuleFor(command => command.Date).NotNull().NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken) =>
            (await CheckIfAccountExits(request, cancellationToken)
                .OnSuccess(() => CheckIfCategoryExist(request, cancellationToken))
                .OnSuccess(() => GetExistingTransaction(request, cancellationToken)))
            .OnSuccess(ModifyTransaction(request))
            .OnSuccess(SetTags(request))
            .OnSuccess(SaveTransaction());

        private static Func<Transaction, Result<Transaction>> ModifyTransaction(Request request) =>
            transaction => transaction.Modify(
                date: request.Date,
                amount: request.Amount,
                description: request.Description,
                categoryId: request.CategoryId,
                accountId: request.AccountId
            );

        private async Task<Result<Transaction>> GetExistingTransaction(Request request,
            CancellationToken cancellationToken)
        {
            var transaction = await context.Transactions.SingleOrDefaultAsync(
                    x => x.Id == request.Id && x.UserId == userContext.Id, cancellationToken);
            return transaction is null
                ? Result<Transaction>.Failure(TransactionErrors.NotFound(request.Id))
                : Result<Transaction>.Success(transaction);
        }

        private async Task<Result> CheckIfCategoryExist(Request request, CancellationToken cancellationToken)
        {
            if (!await context.Categories.AnyAsync(x => x.Id == request.CategoryId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                return Result.Failure(TransactionErrors.CategoryNotFound(request.CategoryId));
            }

            return Result.Success();
        }

        private async Task<Result> CheckIfAccountExits(Request request, CancellationToken cancellationToken)
        {
            if (!await context.Accounts.AnyAsync(x => x.Id == request.AccountId && x.UserId == userContext.Id,
                    cancellationToken))
            {
                return Result.Failure(TransactionErrors.AccountNotFound(request.AccountId));
            }

            return Result.Success();
        }

        private static Func<Transaction, Result<Transaction>> SetTags(Request request) =>
            transaction => transaction.SetTags(request.Tags);

        private Func<Transaction, Result> SaveTransaction() =>
            transaction =>
            {
                context.Update(transaction);
                return Result.Success();
            };
    }
}
