using HazelnutQL.Core.HazelnutQL.Core.Execution.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Execution;

public class ResolveFieldContext : IResolveFieldContext
{
    public object? Source { get; init; }

    public IReadOnlyDictionary<string, object?> Arguments { get; init; } = new Dictionary<string, object?>();

    public CancellationToken CancellationToken { get; init; }
}