using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Transactions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Transactions.Commands;

public class DeleteTransaction : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapDelete("api/v2/transactions/{id}",
                async (TransactionId id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new Request(id), cancellationToken);
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
            .WithName(nameof(DeleteTransaction))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request(TransactionId Id) : IRequest<Result>, IUnitOfWorkRequest;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(command => command.Id).NotNull().NotEqual(TransactionId.Empty);
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            var transaction =
                await context.Transactions.SingleOrDefaultAsync(
                    x => x.Id == request.Id && x.UserId == userContext.Id, cancellationToken);
            if (transaction is null)
            {
                return Result.Failure(TransactionErrors.NotFound(request.Id));
            }

            context.Remove(transaction);

            return Result.Success();
        }
    }
}
