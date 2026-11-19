using System.Text.Json;
using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class ScalarType : IInputType, IOutputType
{
    private readonly Func<object?, object?> _parseValue;
    private readonly Func<object?, object?> _serialize;

    public ScalarType(string name, Func<object?, object?> parseValue, Func<object?, object?> serialize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _parseValue = parseValue;
        _serialize = serialize;
    }

    public string Name { get; }

    /// <summary>Coerces an external (variable) value to its internal representation.</summary>
    public object? ParseValue(object? value) => IsNull(value) ? null : _parseValue(value!);

    /// <summary>Coerces a resolver result to its output representation.</summary>
    public object? Serialize(object? value) => IsNull(value) ? null : _serialize(value!);

    private static bool IsNull(object? value) =>
        value is null or JsonElement { ValueKind: JsonValueKind.Null };

    public override string ToString() => Name;
}