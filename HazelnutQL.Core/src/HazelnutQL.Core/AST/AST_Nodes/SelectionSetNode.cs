using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class SelectionSetNode : ASTNode
{
    public IReadOnlyList<ISelectionNode> Selections { get; init; } = [];
}
