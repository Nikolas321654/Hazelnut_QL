using HazelnutQL.Core.HazelnutQL.Core.Parsing;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;
using LexerType = HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer.Lexer;

namespace HazelnutQL.Core.Tests.Parsing.Lexer;

public class LexerStringTests
{
    private static List<Token> Lex(string source) => new LexerType(source).Tokenize();

    private static string LexSingleString(string source)
    {
        List<Token> tokens = Lex(source);
        Assert.Equal(TokenKind.String, tokens[0].TokenKind);
        return tokens[0].Value;
    }

    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"hello world\"", "hello world")]
    [InlineData("\"tab\there\"", "tab\there")]
    [InlineData("\"h\u00E9llo \u2603 \u043F\u0440\u0438\u0432\u0456\u0442\"", "h\u00E9llo \u2603 \u043F\u0440\u0438\u0432\u0456\u0442")]
    [InlineData("\"\uD83D\uDE00\"", "\uD83D\uDE00")]
    [InlineData("\"{ not: punctuation }\"", "{ not: punctuation }")]
    public void Tokenize_SimpleString_ReturnsContentWithoutQuotes(string source, string expected)
    {
        Assert.Equal(expected, LexSingleString(source));
    }

    [Theory]
    [InlineData("\"\\\"\"", "\"")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\/\"", "/")]
    [InlineData("\"\\b\"", "\b")]
    [InlineData("\"\\f\"", "\f")]
    [InlineData("\"\\n\"", "\n")]
    [InlineData("\"\\r\"", "\r")]
    [InlineData("\"\\t\"", "\t")]
    [InlineData("\"a\\nb\"", "a\nb")]
    public void Tokenize_EscapeSequence_IsDecoded(string source, string expected)
    {
        Assert.Equal(expected, LexSingleString(source));
    }

    [Theory]
    [InlineData("\"\\u0041\"", "A")]
    [InlineData("\"\\u00e9\"", "\u00E9")]
    [InlineData("\"\\u00E9\"", "\u00E9")]
    [InlineData("\"\\u2603\"", "\u2603")]
    [InlineData("\"\\uD83D\\uDE00\"", "\uD83D\uDE00")]
    [InlineData("\"x\\u0041y\"", "xAy")]
    public void Tokenize_UnicodeEscape_IsDecodedWithHexCaseInsensitivity(string source, string expected)
    {
        Assert.Equal(expected, LexSingleString(source));
    }

    [Theory]
    [InlineData("\"\\u12G4\"")]
    [InlineData("\"\\u12\"")]
    [InlineData("\"\\u\"")]
    [InlineData("\"\\uZZZZ\"")]
    public void Tokenize_InvalidUnicodeEscape_Throws(string source)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Contains("Invalid Unicode escape sequence", ex.Message);
    }

    [Fact]
    public void Tokenize_InvalidUnicodeEscape_ReportsLocationOfBackslash()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("  \"ab\\u12G4\""));

        Assert.Equal(1, ex.Location.Line);
        Assert.Equal(6, ex.Location.Column);
    }

    [Theory]
    [InlineData("\"\\x\"", 'x')]
    [InlineData("\"\\a\"", 'a')]
    [InlineData("\"\\0\"", '0')]
    [InlineData("\"\\ \"", ' ')]
    public void Tokenize_UnknownEscape_ThrowsNamingTheCharacter(string source, char escaped)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Contains($"Invalid character escape sequence: '\\{escaped}'", ex.Message);
    }

    [Fact]
    public void Tokenize_UnknownEscape_ReportsLocationOfBackslash()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("\"a\\x\""));

        Assert.Equal((1, 3), (ex.Location.Line, ex.Location.Column));
    }

    [Theory]
    [InlineData("\"")]
    [InlineData("\"abc")]
    [InlineData("\"ab\ncd\"")]
    [InlineData("\"ab\rcd\"")]
    [InlineData("\"ab\r\ncd\"")]
    public void Tokenize_UnterminatedString_ThrowsAtOpeningQuote(string source)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Equal((1, 1), (ex.Location.Line, ex.Location.Column));
    }

    [Fact]
    public void Tokenize_BackslashAtEndOfInput_ThrowsSyntaxException()
    {
        Assert.Throws<GraphQLSyntaxException>(() => Lex("\"abc\\"));
    }

    [Fact]
    public void Tokenize_UnterminatedString_MessageIsDescriptive()
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex("\"abc"));

        Assert.Contains("Unterminated string literal", ex.Message);
    }

    [Theory]
    [InlineData('\u0001', "U+0001")]
    [InlineData('\u001F', "U+001F")]
    [InlineData('\0', "U+0000")]
    public void Tokenize_ControlCharacterInString_ThrowsWithCodePointAndLocation(char control, string codePoint)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex($"\"{control}\""));

        Assert.Contains($"Invalid character within string: {codePoint}", ex.Message);
        Assert.Equal((1, 2), (ex.Location.Line, ex.Location.Column));
    }

    [Fact]
    public void Tokenize_StringToken_SpansFromOpeningToClosingQuote()
    {
        List<Token> tokens = Lex("  \"ab\" x");

        Assert.Equal(new SourceLocation(1, 3, 2, 6), tokens[0].SourceLocation);
        Assert.Equal((1, 8), (tokens[1].SourceLocation.Line, tokens[1].SourceLocation.Column));
    }

    [Fact]
    public void Tokenize_AdjacentStrings_AreSeparateTokens()
    {
        List<Token> tokens = Lex("\"a\"\"b\"");

        Assert.Equal(["a", "b"], tokens.Take(2).Select(t => t.Value));
    }

    [Fact]
    public void Tokenize_EmptyStringFollowedByQuote_IsNotMistakenForBlockString()
    {
        List<Token> tokens = Lex("\"\" \"x\"");

        Assert.Equal(["", "x"], tokens.Take(2).Select(t => t.Value));
    }

    [Theory]
    [InlineData("\"\"\"hello\"\"\"", "hello")]
    [InlineData("\"\"\"\"\"\"", "")]
    [InlineData("\"\"\"a \"b\" c\"\"\"", "a \"b\" c")]
    [InlineData("\"\"\"a\\nb\"\"\"", "a\\nb")]
    [InlineData("\"\"\"a\\\"\"\"b\"\"\"", "a\"\"\"b")]
    [InlineData("\"\"\"\u00E9 \u2603\"\"\"", "\u00E9 \u2603")]
    public void Tokenize_BlockString_KeepsContentVerbatim(string source, string expected)
    {
        Assert.Equal(expected, LexSingleString(source));
    }

    [Fact]
    public void Tokenize_BlockString_RemovesCommonIndentAndSurroundingBlankLines()
    {
        string value = LexSingleString("\"\"\"\n    Hello\n      World\n    \"\"\"");

        Assert.Equal("Hello\n  World", value);
    }

    [Fact]
    public void Tokenize_BlockString_FirstLineDoesNotContributeToIndent()
    {
        string value = LexSingleString("\"\"\"  first\n    second\"\"\"");

        Assert.Equal("  first\nsecond", value);
    }

    [Fact]
    public void Tokenize_BlockString_PreservesInteriorBlankLines()
    {
        string value = LexSingleString("\"\"\"\n    a\n\n    b\n\"\"\"");

        Assert.Equal("a\n\nb", value);
    }

    [Fact]
    public void Tokenize_BlockString_NormalizesCrLfAndCrToLf()
    {
        Assert.Equal("a\nb", LexSingleString("\"\"\"\r\n  a\r\n  b\r\n\"\"\""));
        Assert.Equal("a\nb", LexSingleString("\"\"\"\r  a\r  b\r\"\"\""));
    }

    [Fact]
    public void Tokenize_BlockString_TabsCountAsIndentation()
    {
        string value = LexSingleString("\"\"\"\n\t\ta\n\t\t\tb\n\"\"\"");

        Assert.Equal("a\n\tb", value);
    }

    [Fact]
    public void Tokenize_BlockString_OnlyBlankLines_ProducesEmptyString()
    {
        Assert.Equal(string.Empty, LexSingleString("\"\"\"\n   \n\t\n\"\"\""));
    }

    [Fact]
    public void Tokenize_BlockString_ShorterWhitespaceLineBecomesEmpty()
    {
        string value = LexSingleString("\"\"\"\n    a\n  \n    b\n\"\"\"");

        Assert.Equal("a\n\nb", value);
    }

    [Fact]
    public void Tokenize_BlockString_ContainingLineBreaks_AdvancesLineNumbersForFollowingTokens()
    {
        List<Token> tokens = Lex("\"\"\"\nline\n\"\"\" next");

        Assert.Equal(TokenKind.String, tokens[0].TokenKind);
        Assert.Equal((1, 1), (tokens[0].SourceLocation.Line, tokens[0].SourceLocation.Column));
        Assert.Equal("next", tokens[1].Value);
        Assert.Equal((3, 5), (tokens[1].SourceLocation.Line, tokens[1].SourceLocation.Column));
    }

    [Theory]
    [InlineData("\"\"\"abc")]
    [InlineData("\"\"\"abc\"\"")]
    [InlineData("\"\"\"")]
    [InlineData("\"\"\"abc\\\"\"\"")]
    public void Tokenize_UnterminatedBlockString_ThrowsAtOpeningQuotes(string source)
    {
        GraphQLSyntaxException ex = Assert.Throws<GraphQLSyntaxException>(() => Lex(source));

        Assert.Contains("Unterminated block string", ex.Message);
        Assert.Equal((1, 1), (ex.Location.Line, ex.Location.Column));
    }

    [Fact]
    public void Tokenize_BlockStringAllowsControlCharactersAndNewlines()
    {
        string value = LexSingleString("\"\"\"a\u0001b\"\"\"");

        Assert.Equal("a\u0001b", value);
    }
}
