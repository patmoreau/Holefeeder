using Holefeeder.Domain.Features.Transactions;
using Holefeeder.Domain.SeedWork;

using Microsoft.AspNetCore.Http;

namespace Holefeeder.Application.Features.Transactions.Exceptions;

public class TransactionNotFoundException : DomainException
{
    public TransactionNotFoundException(Guid id) : base(StatusCodes.Status404NotFound,
        $"{nameof(Transaction)} '{id}' not found")
    {
    }

    public TransactionNotFoundException()
    {
    }

    public TransactionNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public TransactionNotFoundException(string message) : base(message)
    {
    }

    public override string Context => nameof(Transactions);
}
