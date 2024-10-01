using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Accounts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Accounts.Queries;

public class GetAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/accounts/{id}",
                async (AccountId id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new Request(AccountId.Create(id)), cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.Ok(result.Value)
                    };
                })
            .Produces<AccountViewModel>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(nameof(Accounts))
            .WithName(nameof(GetAccount))
            .RequireAuthorization(Policies.ReadUser);

    internal record Request(AccountId Id) : IRequest<Result<AccountViewModel>>;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Id).NotNull().NotEqual(AccountId.Empty);
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<AccountViewModel>>
    {
        public async Task<Result<AccountViewModel>> Handle(Request query, CancellationToken cancellationToken)
        {
            var account = await context.Accounts
                .Include(e => e.Transactions)
                .ThenInclude(e => e.Category)
                .SingleOrDefaultAsync(x => x.Id == query.Id && x.UserId == userContext.Id,
                    cancellationToken);
            if (account is null)
            {
                return Result<AccountViewModel>.Failure(AccountErrors.NotFound(query.Id));
            }

            return Result<AccountViewModel>.Success(new AccountViewModel(
                account.Id,
                account.Type,
                account.Name,
                account.OpenBalance,
                account.OpenDate,
                account.Transactions.Count,
                account.CalculateBalance(),
                account.CalculateLastTransactionDate(),
                account.Description,
                account.Favorite,
                account.Inactive
            ));
        }
    }
}
