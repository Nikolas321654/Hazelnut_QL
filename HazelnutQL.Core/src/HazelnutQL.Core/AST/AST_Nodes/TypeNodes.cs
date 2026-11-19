namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public abstract class TypeNode : ASTNode;

public sealed class NamedTypeNode : TypeNode
{
    public required string Name { get; init; }
}

public sealed class ListTypeNode : TypeNode
{
    public required TypeNode ElementType { get; init; }
}

public sealed class NonNullTypeNode : TypeNode
{
    public required TypeNode InnerType { get; init; }
}
