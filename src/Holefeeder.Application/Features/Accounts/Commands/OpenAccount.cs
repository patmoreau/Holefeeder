using DrifterApps.Seeds.Application.Mediatr;
using DrifterApps.Seeds.Domain;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.Features.Accounts.Queries;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Accounts;
using Holefeeder.Domain.ValueObjects;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.Accounts.Commands;

public class OpenAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("api/v2/accounts/open-account",
                async (Request request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(request, cancellationToken);
                    return result switch
                    {
                        { IsFailure: true } => result.Error.ToProblem(),
                        _ => Results.CreatedAtRoute(nameof(GetAccount), new { Id = (Guid)result.Value },
                            new { Id = (Guid)result.Value })
                    };
                })
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Accounts))
            .WithName(nameof(OpenAccount))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request(AccountType Type, string Name, DateOnly OpenDate, Money OpenBalance, string Description)
        : IRequest<Result<AccountId>>, IUnitOfWorkRequest;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Type).NotNull();
            RuleFor(command => command.Name).NotNull().NotEmpty().Length(1, 255);
            RuleFor(command => command.OpenDate).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context)
        : IRequestHandler<Request, Result<AccountId>>
    {
        public async Task<Result<AccountId>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await context.Accounts.AnyAsync(x => x.Name == request.Name && x.UserId == userContext.Id,
                    cancellationToken))
            {
                return Result<AccountId>.Failure(AccountErrors.NameAlreadyExists(request.Name));
            }

            var result = Account.Create(request.Type, request.Name, request.OpenBalance, request.OpenDate,
                request.Description, userContext.Id);

            if (result.IsFailure)
            {
                return Result<AccountId>.Failure(result.Error);
            }

            await context.Accounts.AddAsync(result.Value, cancellationToken);

            return Result<AccountId>.Success(result.Value.Id);
        }
    }
}
