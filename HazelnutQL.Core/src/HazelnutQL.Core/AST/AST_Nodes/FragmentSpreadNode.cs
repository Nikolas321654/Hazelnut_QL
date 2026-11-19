using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class FragmentSpreadNode : ASTNode, ISelectionNode
{
    public required string Name { get; init; }
    public IReadOnlyList<DirectiveNode> Directives { get; init; } = [];
}
