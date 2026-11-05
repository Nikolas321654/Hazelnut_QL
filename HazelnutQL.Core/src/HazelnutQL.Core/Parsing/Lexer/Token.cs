namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public class Token
{
    public string Value { get; set; }
    public TokenKind TokenKind { get; set; }
    public SourceLocation SourceLocation { get; set; }
}
