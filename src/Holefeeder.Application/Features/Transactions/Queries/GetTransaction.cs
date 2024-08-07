using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Features.Transactions.Exceptions;
using Holefeeder.Application.Models;
using Holefeeder.Application.UserContext;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Queries;

public class GetTransaction : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/transactions/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var requestResult = await mediator.Send(new Request(id), cancellationToken);
                    return Results.Ok(requestResult);
                })
            .Produces<TransactionInfoViewModel>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Transactions))
            .WithName(nameof(GetTransaction))
            .RequireAuthorization(Policies.ReadUser);

    internal record Request(Guid Id) : IRequest<TransactionInfoViewModel>;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Id).NotEmpty();
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, TransactionInfoViewModel>
    {
        public async Task<TransactionInfoViewModel> Handle(Request query,
            CancellationToken cancellationToken)
        {
            var transaction = await context.Transactions
                .Include(x => x.Account)
                .Include(x => x.Category)
                .SingleOrDefaultAsync(x => x.Id == query.Id && x.UserId == userContext.Id, cancellationToken);
            if (transaction is null)
            {
                throw new TransactionNotFoundException(query.Id);
            }

            return TransactionMapper.MapToDto(transaction);
        }
    }
}
