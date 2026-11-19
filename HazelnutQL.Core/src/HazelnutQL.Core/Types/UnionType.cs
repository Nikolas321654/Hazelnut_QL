using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class UnionType : IOutputType
{
    private readonly List<ObjectType> _possibleTypes = [];

    public UnionType(string name, Func<object, ObjectType?> resolveType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resolveType);
        Name = name;
        ResolveType = resolveType;
    }

    public string Name { get; }

    public Func<object, ObjectType?> ResolveType { get; }

    public IReadOnlyList<ObjectType> PossibleTypes => _possibleTypes;

    public UnionType AddPossibleType(ObjectType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_possibleTypes.Contains(type))
        {
            throw new InvalidOperationException($"Union '{Name}' already contains type '{type.Name}'.");
        }

        _possibleTypes.Add(type);
        return this;
    }

    public bool Contains(ObjectType type) => _possibleTypes.Contains(type);

    public override string ToString() => Name;
}