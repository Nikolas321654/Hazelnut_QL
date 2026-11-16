using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public abstract class ASTNode
{
    public SourceLocation Location { get; init; }
}
