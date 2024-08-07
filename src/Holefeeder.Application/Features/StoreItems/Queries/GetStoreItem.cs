using Holefeeder.Application.Authorization;
using Holefeeder.Application.Context;
using Holefeeder.Application.Features.StoreItems.Exceptions;
using Holefeeder.Application.UserContext;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Holefeeder.Application.Features.StoreItems.Queries;

public class GetStoreItem : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("api/v2/store-items/{id}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new Request(id), cancellationToken);
                    return Results.Ok(result);
                })
            .Produces<StoreItemViewModel>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .WithTags(nameof(StoreItems))
            .WithName(nameof(GetStoreItem))
            .RequireAuthorization(Policies.ReadUser);

    internal record Request(Guid Id) : IRequest<StoreItemViewModel>;

    internal class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Id).NotEmpty();
    }

    internal class Handler(IUserContext userContext, BudgetingContext context) : IRequestHandler<Request, StoreItemViewModel>
    {
        private readonly BudgetingContext _context = context;
        private readonly IUserContext _userContext = userContext;

        public async Task<StoreItemViewModel> Handle(Request request, CancellationToken cancellationToken)
        {
            var result = await _context.StoreItems
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == _userContext.Id,
                    cancellationToken);

            if (result is null)
            {
                throw new StoreItemNotFoundException(request.Id);
            }

            return StoreItemMapper.MapToDto(result);
        }
    }
}
