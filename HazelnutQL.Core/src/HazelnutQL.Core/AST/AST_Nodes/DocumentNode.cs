using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public sealed class DocumentNode : ASTNode
{
    public IReadOnlyList<IDefinitionNode> Definitions { get; init; } = [];
}
