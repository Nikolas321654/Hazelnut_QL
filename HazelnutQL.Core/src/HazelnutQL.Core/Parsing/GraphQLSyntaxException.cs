using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing;

public class GraphQLSyntaxException(string message, SourceLocation location)
    : Exception($"Syntax Error: {message} ({location.Line}:{location.Column})")
{
    public SourceLocation Location { get; } = location;
}
