public enum TokenKind
{
    StartOfFile,
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
    String
}
