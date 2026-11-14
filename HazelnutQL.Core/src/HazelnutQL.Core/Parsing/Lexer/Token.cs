namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public class Token
{
    public string Value { get; init; } = string.Empty;
    public TokenKind TokenKind { get; init; }
    public SourceLocation SourceLocation { get; init; }

    public override string ToString()
    {
        return Value.Length == 0 ? TokenKind.ToString() : $"{TokenKind} '{Value}'";
    }
}
