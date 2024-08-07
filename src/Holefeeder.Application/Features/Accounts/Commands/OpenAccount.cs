using DrifterApps.Seeds.Application.Mediatr;

using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Features.Accounts.Queries;
using Holefeeder.Application.UserContext;
using Holefeeder.Domain.Features.Accounts;

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
                    return Results.CreatedAtRoute(nameof(GetAccount), new { Id = result }, new { Id = result });
                })
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(Accounts))
            .WithName(nameof(OpenAccount))
            .RequireAuthorization(Policies.WriteUser);

    internal record Request(AccountType Type, string Name, DateOnly OpenDate, decimal OpenBalance, string Description)
        : IRequest<Guid>, IUnitOfWorkRequest;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(command => command.Type).NotNull();
            RuleFor(command => command.Name).NotNull().NotEmpty().Length(1, 255);
            RuleFor(command => command.OpenDate).NotEmpty();
        }
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, Guid>
    {
        public async Task<Guid> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await context.Accounts.AnyAsync(x => x.Name == request.Name && x.UserId == userContext.Id,
                    cancellationToken))
            {
                throw new AccountDomainException($"Name '{request.Name}' already exists.");
            }

            var account = Account.Create(request.Type, request.Name, request.OpenBalance, request.OpenDate,
                request.Description, userContext.Id);

            await context.Accounts.AddAsync(account, cancellationToken);

            return account.Id;
        }
    }
}
