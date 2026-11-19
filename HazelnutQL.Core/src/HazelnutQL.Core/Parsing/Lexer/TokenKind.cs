namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public enum TokenKind
{
    EndOfFile,

    Bang,       // !
    Dollar,     // $
    Ampersand,  // &
    ParenL,     // (
    ParenR,     // )
    Spread,     // ...
    Colon,      // :
    Equals,     // =
    At,         // @
    BracketL,   // [
    BracketR,   // ]
    BraceL,     // {
    Pipe,       // |
    BraceR,     // }

    Name,
    Int,
    Float,
    String,
    Boolean,
    Null
}
