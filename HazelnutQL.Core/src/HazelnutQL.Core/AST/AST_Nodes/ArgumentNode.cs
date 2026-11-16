namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class ArgumentNode : ASTNode
{
    public required string Name { get; init; }
    public required ValueNode Value { get; init; }
}

public sealed class DirectiveNode : ASTNode
{
    public required string Name { get; init; }
    public IReadOnlyList<ArgumentNode> Arguments { get; init; } = [];
}

public sealed class VariableDefinitionNode : ASTNode
{
    public required string Name { get; init; }
    public required TypeNode Type { get; init; }
    public ValueNode? DefaultValue { get; init; }
}
