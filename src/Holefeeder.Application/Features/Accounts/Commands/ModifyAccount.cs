using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.ValueObjects;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Accounts.Commands;

public class ModifyAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/accounts/modify-account",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.NoContent()
                    };
                })
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(nameof(Accounts))
            .WithName(nameof(ModifyAccount))
            .RequireAuthorization(Policies.WriteUser);

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            var exists = await context.Accounts
                .SingleOrDefaultAsync(x => x.Id == request.Id && x.UserId == userContext.Id, cancellationToken);
            if (exists is null)
            {
                return Result.Failure(AccountErrors.NotFound(request.Id));
            }

            var result = exists.Modify(name: request.Name, openBalance: request.OpenBalance, description: request.Description);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }
            context.Update(result.Value);

            return Result.Success();
        }
    }

    internal record Request(AccountId Id, string Name, Money OpenBalance, string Description) : IRequest<Result>, IUnitOfWorkRequest;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Id).NotNull().NotEqual(AccountId.Empty);
            RuleFor(command => command.Name).NotNull().NotEmpty().Length(1, 255);
        }
    }
}
