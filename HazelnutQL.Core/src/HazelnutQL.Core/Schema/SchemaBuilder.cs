using HazelnutQL.Core.HazelnutQL.Core.Execution.Interfaces;
using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public static class TypeExtensions
{
    public static NonNullType NonNull(this IGraphQLType type) => new(type);

    public static ListType ListOf(this IGraphQLType type) => new(type);
}

public sealed class ObjectTypeBuilder
{
    private readonly ObjectType _type;

    internal ObjectTypeBuilder(ObjectType type) => _type = type;

    public ObjectTypeBuilder Field(
        string name,
        IGraphQLType type,
        Func<IResolveFieldContext, ValueTask<object?>>? resolver = null,
        params ArgumentDefinition[] arguments)
    {
        _type.AddField(new FieldDefinition(name, type, resolver, arguments));
        return this;
    }

    public ObjectTypeBuilder Field(
        string name,
        IGraphQLType type,
        Func<IResolveFieldContext, object?> resolver,
        params ArgumentDefinition[] arguments)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return Field(name, type, ctx => ValueTask.FromResult(resolver(ctx)), arguments);
    }
}

public sealed class SchemaBuilder
{
    private ObjectType? _query;
    private ObjectType? _mutation;

    public static SchemaBuilder Create() => new();

    public ObjectType Object(string name, Action<ObjectTypeBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var type = new ObjectType(name);
        configure(new ObjectTypeBuilder(type));
        return type;
    }

    public UnionType Union(string name, Func<object, ObjectType?> resolveType, params ObjectType[] members)
    {
        var union = new UnionType(name, resolveType);
        foreach (var member in members)
        {
            union.AddPossibleType(member);
        }

        return union;
    }

    public SchemaBuilder Query(ObjectType query)
    {
        _query = query;
        return this;
    }

    public SchemaBuilder Query(string name, Action<ObjectTypeBuilder> configure) => Query(Object(name, configure));

    public SchemaBuilder Mutation(ObjectType mutation)
    {
        _mutation = mutation;
        return this;
    }

    public SchemaBuilder Mutation(string name, Action<ObjectTypeBuilder> configure) =>
        Mutation(Object(name, configure));

    public Schema Build()
    {
        if (_query is null)
        {
            throw new InvalidOperationException("Schema requires a Query root type.");
        }

        return new Schema(_query, _mutation);
    }
}