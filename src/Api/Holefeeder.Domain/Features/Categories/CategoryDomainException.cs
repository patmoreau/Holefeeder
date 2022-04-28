using Holefeeder.Domain.SeedWork;

using Microsoft.AspNetCore.Http;

namespace Holefeeder.Domain.Features.Categories;

public class CategoryDomainException : DomainException
{
    public CategoryDomainException(string message) : base(StatusCodes.Status422UnprocessableEntity, message)
    {
    }

    public override string Context => nameof(Category);
}