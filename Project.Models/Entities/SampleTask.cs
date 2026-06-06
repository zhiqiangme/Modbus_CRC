namespace Project.Models;

public sealed class SampleTask
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Owner { get; init; }

    public required string Status { get; init; }
}
