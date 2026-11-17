using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class FieldNode : ASTNode, ISelectionNode
{
    public string? Alias { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<ArgumentNode> Arguments { get; init; } = [];
    public IReadOnlyList<DirectiveNode> Directives { get; init; } = [];
    public SelectionSetNode? SelectionSet { get; init; }

    /// <summary>The key under which the field appears in the result.</summary>
    public string ResponseKey => Alias ?? Name;
}
