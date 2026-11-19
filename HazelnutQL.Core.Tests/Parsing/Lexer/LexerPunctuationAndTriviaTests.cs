using HazelnutQL.Core.HazelnutQL.Core.Parsing;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;
using LexerType = HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer.Lexer;

namespace HazelnutQL.Core.Tests.Parsing.Lexer;

public class LexerPunctuationAndTriviaTests
{
    private static List<Token> Lex(string? source) => new LexerType(source!).Tokenize();

    [Theory]
    [InlineData("!", TokenKind.Bang)]
    [InlineData("$", TokenKind.Dollar)]
    [InlineData("&", TokenKind.Ampersand)]
    [InlineData("(", TokenKind.ParenL)]
    [InlineData(")", TokenKind.ParenR)]
    [InlineData("...", TokenKind.Spread)]
    [InlineData(":", TokenKind.Colon)]
    [InlineData("=", TokenKind.Equals)]
    [InlineData("@", TokenKind.At)]
    [InlineData("[", TokenKind.BracketL)]
    [InlineData("]", TokenKind.BracketR)]
    [InlineData("{", TokenKind.BraceL)]
    [InlineData("|", TokenKind.Pipe)]
    [InlineData("}", TokenKind.BraceR)]
    public void Tokenize_SinglePunctuator_ProducesMatchingKindAndLexeme(string source, TokenKind expectedKind)
    {
        List<Token> tokens = Lex(source);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedKind, tokens[0].TokenKind);
        Assert.Equal(source, tokens[0].Value);
        Assert.Equal(new SourceLocation(1, 1, 0, source.Length), tokens[0].SourceLocation);
        Assert.Equal(TokenKind.EndOfFile, tokens[1].TokenKind);
    }

    [Fact]
    public void Tokenize_AdjacentPunctuatorsWithoutSpaces_SplitsEachOne()
    {
        List<Token> tokens = Lex("{}[]()!$&:=@|");

        Assert.Equal(
        [
            TokenKind.BraceL, TokenKind.BraceR, TokenKind.BracketL, TokenKind.BracketR, TokenKind.ParenL,
            TokenKind.ParenR, TokenKind.Bang, TokenKind.Dollar, TokenKind.Ampersand, TokenKind.Colon,
            TokenKind.Equals, TokenKind.At, TokenKind.Pipe, TokenKind.EndOfFile
        ], tokens.Select(t => t.TokenKind));
    }

    [Fact]
    public void Tokenize_SpreadFollowedByName_ProducesSpreadThenName()
    {
        List<Token> tokens = Lex("...Frag");

        Assert.Equal([TokenKind.Spread, TokenKind.Name, TokenKind.EndOfFile], tokens.Select(t => t.TokenKind));
        Assert.Equal("Frag", tokens[1].Value);
        Assert.Equal(4, tokens[1].SourceLocation.Column);
    }

    [Theory]
    [InlineData(".", 1, 1)]
    [InlineData("..", 1, 1)]
    [InlineData(".a", 1, 1)]
    [InlineData("  ..a", 1, 3)]
    [InlineData("\n\n. .", 3, 1)]
    public void Tokenize_IncompleteSpread_ThrowsAtDotLocation(string source, int line, int column)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Equal(line, ex.Location.Line);
        Assert.Equal(column, ex.Location.Column);
        Assert.Contains("Did you mean '...'", ex.Message);
    }

    [Theory]
    [InlineData("?", '?', 1, 1)]
    [InlineData("   ?", '?', 1, 4)]
    [InlineData("a\n  %", '%', 2, 3)]
    [InlineData("\u00FC", '\u00FC', 1, 1)]
    [InlineData("\u2603", '\u2603', 1, 1)]
    [InlineData("\0", '\0', 1, 1)]
    public void Tokenize_UnexpectedCharacter_ThrowsWithCharacterAndLocation(string source, char unexpected, int line, int column)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Equal(line, ex.Location.Line);
        Assert.Equal(column, ex.Location.Column);
        Assert.Contains($"Unexpected character '{unexpected}'", ex.Message);
    }

    [Fact]
    public void SyntaxException_Message_IncludesPrefixAndLineColumn()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("\n ?"));

        Assert.Equal("Syntax Error: Unexpected character '?'. (2:2)", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData(",,,")]
    [InlineData("# only a comment")]
    [InlineData("\uFEFF")]
    public void Tokenize_OnlyIgnoredInput_ProducesSingleEndOfFile(string source)
    {
        List<Token> tokens = Lex(source);

        Token eof = Assert.Single(tokens);
        Assert.Equal(TokenKind.EndOfFile, eof.TokenKind);
        Assert.Equal(string.Empty, eof.Value);
    }

    [Fact]
    public void Tokenize_NullSource_TreatedAsEmpty()
    {
        List<Token> tokens = Lex(null);

        Assert.Equal(TokenKind.EndOfFile, Assert.Single(tokens).TokenKind);
    }

    [Fact]
    public void Tokenize_CommasAreInsignificant_BetweenAndAroundTokens()
    {
        List<Token> tokens = Lex(",a,,b,");

        Assert.Equal(["a", "b", ""], tokens.Select(t => t.Value));
    }

    [Fact]
    public void Tokenize_ByteOrderMarkAtStart_IsSkipped()
    {
        List<Token> tokens = Lex("\uFEFFquery");

        Assert.Equal(TokenKind.Name, tokens[0].TokenKind);
        Assert.Equal("query", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_CommentUntilNewline_IsSkippedAndNextLineIsTracked()
    {
        List<Token> tokens = Lex("# hello { } \"unterminated\nfoo");

        Assert.Equal("foo", tokens[0].Value);
        Assert.Equal(2, tokens[0].SourceLocation.Line);
        Assert.Equal(1, tokens[0].SourceLocation.Column);
    }

    [Fact]
    public void Tokenize_CommentTerminatedByCarriageReturn_ResumesLexing()
    {
        List<Token> tokens = Lex("# note\rfoo");

        Assert.Equal(TokenKind.Name, tokens[0].TokenKind);
        Assert.Equal("foo", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_CommentAtEndOfFile_ProducesOnlyEndOfFile()
    {
        List<Token> tokens = Lex("a # trailing");

        Assert.Equal([TokenKind.Name, TokenKind.EndOfFile], tokens.Select(t => t.TokenKind));
    }

    [Fact]
    public void Tokenize_HashInsideString_IsNotAComment()
    {
        List<Token> tokens = Lex("\"a # b\" c");

        Assert.Equal("a # b", tokens[0].Value);
        Assert.Equal("c", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_UnicodeInsideComment_IsIgnored()
    {
        List<Token> tokens = Lex("# \u043F\u0440\u0438\u0432\u0456\u0442 \u2603 \uD83D\uDE00\nx");

        Assert.Equal("x", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_WindowsLineEndings_CountOneLinePerCrLf()
    {
        List<Token> tokens = Lex("a\r\nb\r\nc");

        Assert.Equal([1, 2, 3], tokens.Take(3).Select(t => t.SourceLocation.Line));
        Assert.Equal([1, 1, 1], tokens.Take(3).Select(t => t.SourceLocation.Column));
    }

    [Fact]
    public void Tokenize_MultiLineQuery_ReportsAccurateLineAndColumn()
    {
        List<Token> tokens = Lex("query {\n  user(id: 4) {\n    name\n  }\n}");

        Token user = tokens.Single(t => t.Value == "user");
        Token name = tokens.Single(t => t.Value == "name");
        Token last = tokens[^2];

        Assert.Equal((2, 3), (user.SourceLocation.Line, user.SourceLocation.Column));
        Assert.Equal((3, 5), (name.SourceLocation.Line, name.SourceLocation.Column));
        Assert.Equal(TokenKind.BraceR, last.TokenKind);
        Assert.Equal((5, 1), (last.SourceLocation.Line, last.SourceLocation.Column));
    }

    [Fact]
    public void Tokenize_TokenOffsets_AreStartInclusiveEndExclusive()
    {
        const string source = "  foo  12 ";

        List<Token> tokens = Lex(source);

        Assert.Equal(new SourceLocation(1, 3, 2, 5), tokens[0].SourceLocation);
        Assert.Equal(new SourceLocation(1, 8, 7, 9), tokens[1].SourceLocation);
        Assert.Equal("foo", source[tokens[0].SourceLocation.Start..tokens[0].SourceLocation.End]);
        Assert.Equal("12", source[tokens[1].SourceLocation.Start..tokens[1].SourceLocation.End]);
    }

    [Fact]
    public void Tokenize_EndOfFile_IsPositionedAfterTrailingTrivia()
    {
        List<Token> tokens = Lex("a\n  ");

        Token eof = tokens[^1];
        Assert.Equal(TokenKind.EndOfFile, eof.TokenKind);
        Assert.Equal((2, 3), (eof.SourceLocation.Line, eof.SourceLocation.Column));
        Assert.Equal(4, eof.SourceLocation.Start);
    }

    [Fact]
    public void Tokenize_EndsWithExactlyOneEndOfFile()
    {
        List<Token> tokens = Lex("{ a b }");

        Assert.Equal(1, tokens.Count(t => t.TokenKind == TokenKind.EndOfFile));
        Assert.Equal(TokenKind.EndOfFile, tokens[^1].TokenKind);
    }

    [Fact]
    public void NextToken_CalledRepeatedlyAfterEnd_KeepsReturningEndOfFile()
    {
        LexerType lexer = new("a");

        Assert.Equal(TokenKind.Name, lexer.NextToken().TokenKind);
        Assert.Equal(TokenKind.EndOfFile, lexer.NextToken().TokenKind);
        Assert.Equal(TokenKind.EndOfFile, lexer.NextToken().TokenKind);
    }

    [Fact]
    public void NextToken_StreamsTokensOneAtATime()
    {
        LexerType lexer = new("{ a }");

        Assert.Equal(TokenKind.BraceL, lexer.NextToken().TokenKind);
        Assert.Equal("a", lexer.NextToken().Value);
        Assert.Equal(TokenKind.BraceR, lexer.NextToken().TokenKind);
        Assert.Equal(TokenKind.EndOfFile, lexer.NextToken().TokenKind);
    }

    [Fact]
    public void Tokenize_ErrorAfterValidTokens_ThrowsInsteadOfReturningPartialResult()
    {
        Assert.Throws<GraphQLSyntaxException>(() => Lex("{ a ? }"));
    }

    [Fact]
    public void Token_ToString_ShowsKindAndValueOnlyWhenValuePresent()
    {
        List<Token> tokens = Lex("foo");

        Assert.Equal("Name 'foo'", tokens[0].ToString());
        Assert.Equal("EndOfFile", tokens[1].ToString());
    }

    [Fact]
    public void Tokenize_VeryLargeInput_CompletesWithoutStackOverflow()
    {
        string source = string.Concat(Enumerable.Repeat("{ ", 100_000)) + string.Concat(Enumerable.Repeat("} ", 100_000));

        List<Token> tokens = Lex(source);

        Assert.Equal(200_001, tokens.Count);
    }
}
