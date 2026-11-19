using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class Schema
{
    private readonly Dictionary<string, IGraphQLType> _types = new(StringComparer.Ordinal);

    public Schema(ObjectType query, ObjectType? mutation = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        Query = query;
        Mutation = mutation;

        foreach (var scalar in ScalarTypes.All)
        {
            _types[scalar.Name] = scalar;
        }

        Register(query);
        if (mutation is not null)
        {
            Register(mutation);
        }
    }

    public ObjectType Query { get; }

    public ObjectType? Mutation { get; }

    public IReadOnlyDictionary<string, IGraphQLType> Types => _types;

    public IGraphQLType? GetType(string name) => _types.GetValueOrDefault(name);

    private void Register(IGraphQLType type)
    {
        type = Unwrap(type);
        if (_types.TryGetValue(type.Name, out var existing))
        {
            if (!ReferenceEquals(existing, type))
            {
                throw new InvalidOperationException($"Schema contains two different types named '{type.Name}'.");
            }

            return;
        }

        _types[type.Name] = type;

        switch (type)
        {
            case ObjectType obj:
                foreach (var field in obj.Fields.Values)
                {
                    Register(field.Type);
                    foreach (var argument in field.Arguments.Values)
                    {
                        Register(argument.Type);
                    }
                }

                break;
            case UnionType union:
                if (union.PossibleTypes.Count == 0)
                {
                    throw new InvalidOperationException($"Union '{union.Name}' must have at least one member type.");
                }

                foreach (var member in union.PossibleTypes)
                {
                    Register(member);
                }

                break;
        }
    }

    private static IGraphQLType Unwrap(IGraphQLType type)
    {
        while (true)
        {
            switch (type)
            {
                case NonNullType nn:
                    type = nn.InnerType;
                    break;
                case ListType list:
                    type = list.ElementType;
                    break;
                default:
                    return type;
            }
        }
    }
}