using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class ObjectType : IOutputType
{
    private readonly Dictionary<string, FieldDefinition> _fields = new(StringComparer.Ordinal);

    public ObjectType(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, FieldDefinition> Fields => _fields;

    public ObjectType AddField(FieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!_fields.TryAdd(field.Name, field))
        {
            throw new InvalidOperationException($"Type '{Name}' already defines field '{field.Name}'.");
        }

        return this;
    }

    public override string ToString() => Name;
}