using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class NonNullType : IInputType, IOutputType
{
    public NonNullType(IGraphQLType innerType)
    {
        ArgumentNullException.ThrowIfNull(innerType);
        if (innerType is NonNullType)
        {
            throw new ArgumentException("NonNullType cannot wrap another NonNullType.", nameof(innerType));
        }

        InnerType = innerType;
    }

    public IGraphQLType InnerType { get; }

    public string Name => InnerType.Name + "!";

    public override string ToString() => Name;
}