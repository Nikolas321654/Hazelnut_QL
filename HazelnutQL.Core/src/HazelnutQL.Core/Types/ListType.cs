using HazelnutQL.Core.HazelnutQL.Core.Schema.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public class ListType : IInputType, IOutputType
{
    public ListType(IGraphQLType elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
    }

    public IGraphQLType ElementType { get; }

    public string Name => "[" + ElementType.Name + "]";

    public override string ToString() => Name;
}