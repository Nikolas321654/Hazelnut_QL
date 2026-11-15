using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using HazelnutQL.Core.HazelnutQL.Core.Parsing;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;
using LexerType = HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer.Lexer;
using System.Globalization;
using System.Text;

namespace HazelnutQL.Core.Tests.Properties;

public class LexerPropertyTests
{
    private const string NameAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

    private static List<Token> Lex(string source) => new LexerType(source).Tokenize();

    private static string ToName(byte[] seed)
    {
        StringBuilder builder = new("n");
        foreach (byte b in seed)
        {
            builder.Append(NameAlphabet[b % NameAlphabet.Length]);
        }

        return builder.ToString();
    }

    [Property]
    public bool Tokenize_ArbitraryInput_OnlyEverThrowsGraphQLSyntaxException(string? input)
    {
        try
        {
            Lex(input ?? string.Empty);
            return true;
        }
        catch (GraphQLSyntaxException)
        {
            return true;
        }
    }

    [Property]
    public bool Tokenize_ArbitraryInput_WhenSuccessful_EndsWithSingleEofAndHasOrderedInBoundsSpans(string? input)
    {
        string source = input ?? string.Empty;
        List<Token> tokens;
        try
        {
            tokens = Lex(source);
        }
        catch (GraphQLSyntaxException)
        {
            return true;
        }

        bool singleEof = tokens.Count(t => t.TokenKind == TokenKind.EndOfFile) == 1 && tokens[^1].TokenKind == TokenKind.EndOfFile;
        bool spansValid = tokens.All(t => 0 <= t.SourceLocation.Start && t.SourceLocation.Start <= t.SourceLocation.End && t.SourceLocation.End <= source.Length);
        bool ordered = tokens.Zip(tokens.Skip(1), (a, b) => a.SourceLocation.End <= b.SourceLocation.Start).All(x => x);
        bool positive = tokens.All(t => t.SourceLocation.Line >= 1 && t.SourceLocation.Column >= 1);
        return singleEof && spansValid && ordered && positive;
    }

    [Property]
    public bool Tokenize_ArbitraryInput_IsDeterministic(string? input)
    {
        string source = input ?? string.Empty;
        string first = Describe(source);
        string second = Describe(source);
        return first == second;

        static string Describe(string s)
        {
            try
            {
                return string.Join("|", Lex(s).Select(t => $"{t.TokenKind}:{t.Value}:{t.SourceLocation}"));
            }
            catch (GraphQLSyntaxException ex)
            {
                return ex.Message;
            }
        }
    }

    [Property]
    public bool Tokenize_ValidName_RoundTripsAsSingleNameToken(byte[]? seed)
    {
        string name = ToName(seed ?? []);

        List<Token> tokens = Lex(name);

        return tokens.Count == 2 && tokens[0].TokenKind == TokenKind.Name && tokens[0].Value == name;
    }

    [Property]
    public bool Tokenize_Integer_RoundTripsAsIntToken(int value)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);

        List<Token> tokens = Lex(text);

        return tokens.Count == 2 && tokens[0].TokenKind == TokenKind.Int && tokens[0].Value == text;
    }

    [Property]
    public bool Tokenize_FiniteDouble_RoundTripsAsFloatToken(NormalFloat value)
    {
        string text = value.Item.ToString("0.0###############", CultureInfo.InvariantCulture);

        List<Token> tokens = Lex(text);

        return tokens.Count == 2 && tokens[0].TokenKind == TokenKind.Float && tokens[0].Value == text;
    }

    [Property]
    public bool Tokenize_StringWithEveryCharEscapedAsUnicode_DecodesToOriginal(string? original)
    {
        string text = original ?? string.Empty;
        StringBuilder literal = new("\"");
        foreach (char c in text)
        {
            literal.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        literal.Append('"');

        List<Token> tokens = Lex(literal.ToString());

        return tokens[0].TokenKind == TokenKind.String && tokens[0].Value == text;
    }

    [Property]
    public bool Tokenize_ExtraWhitespaceCommasAndComments_DoNotChangeTokenKindsOrValues(byte[]? seedA, byte[]? seedB)
    {
        string a = ToName(seedA ?? []);
        string b = ToName(seedB ?? []);

        List<Token> compact = Lex($"{{{a}:{b}}}");
        List<Token> spaced = Lex($" ,\t{{ # c\n,{a}\r\n,: ,{b}\n}} ,# end");

        return compact.Select(t => (t.TokenKind, t.Value)).SequenceEqual(spaced.Select(t => (t.TokenKind, t.Value)));
    }

    [Property]
    public bool Tokenize_NameTokens_SourceSliceMatchesValue(byte[]? seed)
    {
        string source = $"  # c\n {ToName(seed ?? [])} ";

        Token token = Lex(source)[0];

        return source[token.SourceLocation.Start..token.SourceLocation.End] == token.Value;
    }
}
