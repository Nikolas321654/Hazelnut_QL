using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public enum OperationType
{
    Query,
    Mutation
}

public sealed class OperationDefinitionNode : ASTNode, IDefinitionNode
{
    public OperationType Operation { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; init; } = [];
    public IReadOnlyList<DirectiveNode> Directives { get; init; } = [];
    public required SelectionSetNode SelectionSet { get; init; }
}
