using System.Collections.Immutable;

namespace Holefeeder.Ui.Common.Models;

public record Upcoming
{
    private readonly ImmutableArray<string> _tags = ImmutableArray<string>.Empty;

    public Guid Id { get; init; }

    public DateOnly Date { get; init; }

    public decimal Amount { get; init; }

    public string Description { get; init; } = null!;

    public ImmutableArray<string> Tags
    {
        get => _tags;
        init => _tags = [.. value.ToArray()];
    }

    public CategoryInfo Category { get; init; } = null!;

    public AccountInfo Account { get; init; } = null!;
}
