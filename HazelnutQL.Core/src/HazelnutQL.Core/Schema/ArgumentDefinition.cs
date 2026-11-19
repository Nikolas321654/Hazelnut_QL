using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class ArgumentDefinition
{
    public ArgumentDefinition(string name, IGraphQLType type, object? defaultValue = null, bool hasDefaultValue = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        HasDefaultValue = hasDefaultValue || defaultValue is not null;
    }

    public string Name { get; }

    public IGraphQLType Type { get; }

    public object? DefaultValue { get; }

    public bool HasDefaultValue { get; }
}