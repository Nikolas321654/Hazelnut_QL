using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class FragmentDefinitionNode : ASTNode, IDefinitionNode
{
    public required string Name { get; init; }
    public required NamedTypeNode TypeCondition { get; init; }
    public IReadOnlyList<DirectiveNode> Directives { get; init; } = [];
    public required SelectionSetNode SelectionSet { get; init; }
}
