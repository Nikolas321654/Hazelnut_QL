using HazelnutQL.Core.HazelnutQL.Core.Parsing;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;
using LexerType = HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer.Lexer;

namespace HazelnutQL.Core.Tests.Parsing.Lexer;

public class LexerNameAndNumberTests
{
    private static List<Token> Lex(string source) => new LexerType(source).Tokenize();

    [Theory]
    [InlineData("query")]
    [InlineData("_")]
    [InlineData("_private")]
    [InlineData("__typename")]
    [InlineData("camelCase")]
    [InlineData("a1b2_c3")]
    [InlineData("Z")]
    [InlineData("Null")]
    [InlineData("True")]
    [InlineData("nullable")]
    [InlineData("trueish")]
    [InlineData("falsey")]
    public void Tokenize_Identifier_ProducesNameToken(string name)
    {
        List<Token> tokens = Lex(name);

        Assert.Equal(TokenKind.Name, tokens[0].TokenKind);
        Assert.Equal(name, tokens[0].Value);
    }

    [Theory]
    [InlineData("true", TokenKind.Boolean)]
    [InlineData("false", TokenKind.Boolean)]
    [InlineData("null", TokenKind.Null)]
    public void Tokenize_ReservedLiteral_ProducesLiteralKind(string source, TokenKind kind)
    {
        List<Token> tokens = Lex(source);

        Assert.Equal(kind, tokens[0].TokenKind);
        Assert.Equal(source, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_NameStopsAtNonNameCharacter()
    {
        List<Token> tokens = Lex("user(id:1)");

        Assert.Equal(
            [TokenKind.Name, TokenKind.ParenL, TokenKind.Name, TokenKind.Colon, TokenKind.Int, TokenKind.ParenR, TokenKind.EndOfFile],
            tokens.Select(t => t.TokenKind));
    }

    [Fact]
    public void Tokenize_NameCannotStartWithDigit_LexesAsNumberThenFails()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("1abc"));

        Assert.Contains("unexpected character 'a'", ex.Message);
    }

    [Theory]
    [InlineData("0", TokenKind.Int)]
    [InlineData("-0", TokenKind.Int)]
    [InlineData("7", TokenKind.Int)]
    [InlineData("123", TokenKind.Int)]
    [InlineData("-123", TokenKind.Int)]
    [InlineData("2147483648", TokenKind.Int)]
    [InlineData("0.0", TokenKind.Float)]
    [InlineData("1.5", TokenKind.Float)]
    [InlineData("-1.5", TokenKind.Float)]
    [InlineData("0.25", TokenKind.Float)]
    [InlineData("1e3", TokenKind.Float)]
    [InlineData("1E3", TokenKind.Float)]
    [InlineData("1e+3", TokenKind.Float)]
    [InlineData("1e-3", TokenKind.Float)]
    [InlineData("1.5e10", TokenKind.Float)]
    [InlineData("-1.5E-10", TokenKind.Float)]
    [InlineData("0e0", TokenKind.Float)]
    public void Tokenize_Number_DistinguishesIntFromFloatAndKeepsLexeme(string source, TokenKind kind)
    {
        List<Token> tokens = Lex(source);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(kind, tokens[0].TokenKind);
        Assert.Equal(source, tokens[0].Value);
        Assert.Equal(new SourceLocation(1, 1, 0, source.Length), tokens[0].SourceLocation);
    }

    [Theory]
    [InlineData("1)", TokenKind.Int, TokenKind.ParenR)]
    [InlineData("1,2", TokenKind.Int, TokenKind.Int)]
    [InlineData("1 2", TokenKind.Int, TokenKind.Int)]
    [InlineData("1.5}", TokenKind.Float, TokenKind.BraceR)]
    [InlineData("-1]", TokenKind.Int, TokenKind.BracketR)]
    [InlineData("1\"x\"", TokenKind.Int, TokenKind.String)]
    public void Tokenize_NumberFollowedByDelimiter_EndsNumberCleanly(string source, TokenKind first, TokenKind second)
    {
        List<Token> tokens = Lex(source);

        Assert.Equal(first, tokens[0].TokenKind);
        Assert.Equal(second, tokens[1].TokenKind);
    }

    [Theory]
    [InlineData("01", "unexpected digit after 0")]
    [InlineData("-01", "unexpected digit after 0")]
    [InlineData("00", "unexpected digit after 0")]
    [InlineData("-", "expected a digit")]
    [InlineData("-a", "expected a digit")]
    [InlineData("- 1", "expected a digit")]
    [InlineData("1.", "expected a digit")]
    [InlineData("1.e3", "expected a digit")]
    [InlineData("1e", "expected a digit")]
    [InlineData("1e+", "expected a digit")]
    [InlineData("1e-x", "expected a digit")]
    [InlineData("1.2.3", "unexpected character '.'")]
    [InlineData("1..2", "expected a digit")]
    [InlineData("1a", "unexpected character 'a'")]
    [InlineData("1_", "unexpected character '_'")]
    [InlineData("0x1F", "unexpected character 'x'")]
    [InlineData("1.5f", "unexpected character 'f'")]
    [InlineData("1e5e", "unexpected character 'e'")]
    [InlineData("1e5.0", "unexpected character '.'")]
    public void Tokenize_MalformedNumber_ThrowsSyntaxException(string source, string expectedFragment)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Contains(expectedFragment, ex.Message);
    }

    [Fact]
    public void Tokenize_MalformedNumber_ReportsLocationOfNumberStart()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("a\n  01"));

        Assert.Equal(2, ex.Location.Line);
        Assert.Equal(3, ex.Location.Column);
    }

    [Fact]
    public void Tokenize_NumbersAndNamesInSequence_TrackColumns()
    {
        List<Token> tokens = Lex("x:-12 y:3.5");

        Token negative = tokens.Single(t => t.TokenKind == TokenKind.Int);
        Token real = tokens.Single(t => t.TokenKind == TokenKind.Float);
        Assert.Equal(3, negative.SourceLocation.Column);
        Assert.Equal(9, real.SourceLocation.Column);
    }
}
