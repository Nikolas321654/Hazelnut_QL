namespace HazelnutQL.Core.HazelnutQL.Core.Execution.Interfaces;

public interface IResolveFieldContext
{
    object? Source { get; }

    IReadOnlyDictionary<string, object?> Arguments { get; }

    CancellationToken CancellationToken { get; }
}