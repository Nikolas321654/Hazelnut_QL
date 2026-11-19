using HazelnutQL.Core.HazelnutQL.Core.Execution.Interfaces;
using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class FieldDefinition
{
    public FieldDefinition(
        string name,
        IGraphQLType type,
        Func<IResolveFieldContext, ValueTask<object?>>? resolver = null,
        IEnumerable<ArgumentDefinition>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);
        Name = name;
        Type = type;
        Resolver = resolver;

        var args = new Dictionary<string, ArgumentDefinition>(StringComparer.Ordinal);
        foreach (var argument in arguments ?? [])
        {
            if (!args.TryAdd(argument.Name, argument))
            {
                throw new ArgumentException($"Duplicate argument '{argument.Name}' on field '{name}'.", nameof(arguments));
            }
        }

        Arguments = args;
    }

    public string Name { get; }

    public IGraphQLType Type { get; }

    public Func<IResolveFieldContext, ValueTask<object?>>? Resolver { get; }

    public IReadOnlyDictionary<string, ArgumentDefinition> Arguments { get; }
}