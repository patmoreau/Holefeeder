using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Features.Transactions.Queries;
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

public class Transfer : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/transactions/transfer",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.CreatedAtRoute(nameof(GetTransaction), new { Id = (Guid)result.Value.FromTransactionId },
                            result.Value)
                    };
                })
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(Transfer))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request(
        DateOnly Date,
        Money Amount,
        string Description,
        AccountId FromAccountId,
        AccountId ToAccountId) : IRequest<Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>>,
        IUnitOfWorkRequest;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.FromAccountId).NotNull().NotEqual(AccountId.Empty);
            RuleFor(command => command.ToAccountId).NotNull().NotEqual(AccountId.Empty);
            RuleFor(command => command.Date).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>>
    {
        public Task<Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>> Handle(
            Request request, CancellationToken cancellationToken) =>
            ActiveAccountExistsAsync(request.FromAccountId, cancellationToken)
                .OnSuccess(() => ActiveAccountExistsAsync(request.ToAccountId, cancellationToken))
                .OnSuccess(FindCategoriesAsync(cancellationToken))
                .OnSuccess(CreateTransactionsAsync(request, cancellationToken));

        private async Task<Result> ActiveAccountExistsAsync(AccountId accountId, CancellationToken cancellationToken) =>
            await context.Accounts.AnyAsync(
                x => x.Id == accountId && x.UserId == userContext.Id && !x.Inactive,
                cancellationToken)
                ? Result.Success()
                : Result.Failure(TransactionErrors.AccountNotFound(accountId));

        private Func<Task<Result<(Category TransferFrom, Category TransferTo)>>> FindCategoriesAsync(
            CancellationToken cancellationToken) =>
            async () =>
            {
                const string categoryFromName = "Transfer In";
                const string categoryToName = "Transfer Out";
                var categoryFrom = await context.Categories
                    .FirstOrDefaultAsync(x => x.UserId == userContext.Id && x.Name == categoryFromName,
                        cancellationToken);
                if (categoryFrom is null)
                {
                    return Result<(Category CategoryFrom, Category CategoryTo)>.Failure(
                        TransactionErrors.CategoryNameNotFound(categoryFromName));
                }

                var categoryTo = await context.Categories
                    .FirstOrDefaultAsync(x => x.UserId == userContext.Id && x.Name == categoryToName,
                        cancellationToken);
                return categoryTo is null
                    ? Result<(Category CategoryFrom, Category CategoryTo)>.Failure(
                        TransactionErrors.CategoryNameNotFound(categoryToName))
                    : Result<(Category CategoryFrom, Category CategoryTo)>.Success((categoryFrom, categoryTo));
            };

        private Func<(Category TransferFrom, Category TransferTo),
                Task<Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>>>
            CreateTransactionsAsync(Request request, CancellationToken cancellationToken) =>
            async categories =>
            {
                var transactionFrom = await CreateTransactionAsync(categories.TransferFrom, request, cancellationToken);
                if (transactionFrom.IsFailure)
                {
                    return Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>.Failure(
                        transactionFrom
                            .Error);
                }

                var transactionTo = await CreateTransactionAsync(categories.TransferTo, request, cancellationToken);
                if (transactionTo.IsFailure)
                {
                    return Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>.Failure(
                        transactionTo
                            .Error);
                }

                return Result<(TransactionId FromTransactionId, TransactionId ToTransactionId)>
                    .Success((transactionFrom.Value.Id, transactionTo.Value.Id));
            };

        private async Task<Result<Transaction>> CreateTransactionAsync(Category category, Request request,
            CancellationToken cancellationToken)
        {
            var transaction = Transaction.Create(request.Date, request.Amount, request.Description,
                request.FromAccountId, category.Id, userContext.Id);
            if (transaction.IsFailure)
            {
                return Result<Transaction>.Failure(transaction.Error);
            }

            await context.Transactions.AddAsync(transaction.Value, cancellationToken);
            return Result<Transaction>.Success(transaction.Value);
        }
    }
}
