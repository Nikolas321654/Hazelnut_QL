using System.Text;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public class Lexer(string source)
{
    private int _column = 1;
    private int _line = 1;
    private int _position;
    private readonly string _source = source ?? string.Empty;

    public List<Token> Tokenize()
    {
        List<Token> tokens = [];
        Token token;
        do
        {
            token = NextToken();
            tokens.Add(token);
        } while (token.TokenKind != TokenKind.EndOfFile);

        return tokens;
    }

    private bool IsEnd => _position >= _source.Length;

    private char Read()
    {
        if (IsEnd)
        {
            return '\0';
        }

        char currentChar = _source[_position++];
        if (currentChar == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return currentChar;
    }

    private char Peek()
    {
        return PeekAt(0);
    }

    private char PeekAt(int offset)
    {
        int index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private GraphQLSyntaxException Error(string message, int line, int column)
    {
        return new GraphQLSyntaxException(message, new SourceLocation(line, column, _position, _position));
    }

    private void SkipIgnored()
    {
        while (!IsEnd)
        {
            char c = Peek();

            // BOM, whitespace, line terminators and commas are insignificant
            if (c is ' ' or '\t' or '\r' or '\n' or ',' or '\uFEFF')
            {
                Read();
                continue;
            }

            if (c == '#')
            {
                while (!IsEnd && Peek() != '\n' && Peek() != '\r')
                {
                    Read();
                }

                continue;
            }

            break;
        }
    }

    public Token NextToken()
    {
        SkipIgnored();

        int startPosition = _position;
        int startLine = _line;
        int startColumn = _column;

        if (IsEnd)
        {
            return CreateToken(string.Empty, TokenKind.EndOfFile, startLine, startColumn, startPosition);
        }

        char currentChar = Peek();

        TokenKind? punctuator = currentChar switch
        {
            '!' => TokenKind.Bang,
            '$' => TokenKind.Dollar,
            '&' => TokenKind.Ampersand,
            '(' => TokenKind.ParenL,
            ')' => TokenKind.ParenR,
            ':' => TokenKind.Colon,
            '=' => TokenKind.Equals,
            '@' => TokenKind.At,
            '[' => TokenKind.BracketL,
            ']' => TokenKind.BracketR,
            '{' => TokenKind.BraceL,
            '|' => TokenKind.Pipe,
            '}' => TokenKind.BraceR,
            _ => null
        };

        if (punctuator is not null)
        {
            Read();
            return CreateToken(currentChar.ToString(), punctuator.Value, startLine, startColumn, startPosition);
        }

        if (currentChar == '.')
        {
            if (PeekAt(1) == '.' && PeekAt(2) == '.')
            {
                Read();
                Read();
                Read();
                return CreateToken("...", TokenKind.Spread, startLine, startColumn, startPosition);
            }

            throw Error("Unexpected '.'. Did you mean '...'?", startLine, startColumn);
        }

        if (currentChar == '"')
        {
            string value = PeekAt(1) == '"' && PeekAt(2) == '"'
                ? ReadBlockString(startLine, startColumn)
                : ReadString(startLine, startColumn);
            return CreateToken(value, TokenKind.String, startLine, startColumn, startPosition);
        }

        if (char.IsAsciiDigit(currentChar) || currentChar == '-')
        {
            (string value, TokenKind kind) = ReadNumber(startLine, startColumn);
            return CreateToken(value, kind, startLine, startColumn, startPosition);
        }

        if (IsNameStart(currentChar))
        {
            string value = ReadName();
            TokenKind kind = value switch
            {
                "true" or "false" => TokenKind.Boolean,
                "null" => TokenKind.Null,
                _ => TokenKind.Name
            };
            return CreateToken(value, kind, startLine, startColumn, startPosition);
        }

        throw Error($"Unexpected character '{currentChar}'.", startLine, startColumn);
    }

    private static bool IsNameStart(char c)
    {
        return c == '_' || char.IsAsciiLetter(c);
    }

    private static bool IsNameContinue(char c)
    {
        return c == '_' || char.IsAsciiLetterOrDigit(c);
    }

    private string ReadName()
    {
        int start = _position;
        while (!IsEnd && IsNameContinue(Peek()))
        {
            Read();
        }

        return _source[start.._position];
    }

    private (string Value, TokenKind Kind) ReadNumber(int startLine, int startColumn)
    {
        int start = _position;
        bool isFloat = false;

        if (Peek() == '-')
        {
            Read();
        }

        if (Peek() == '0')
        {
            Read();
            if (char.IsAsciiDigit(Peek()))
            {
                throw Error("Invalid number, unexpected digit after 0.", startLine, startColumn);
            }
        }
        else
        {
            ReadDigits(startLine, startColumn);
        }

        if (Peek() == '.')
        {
            isFloat = true;
            Read();
            ReadDigits(startLine, startColumn);
        }

        if (Peek() is 'e' or 'E')
        {
            isFloat = true;
            Read();

            if (Peek() is '+' or '-')
            {
                Read();
            }

            ReadDigits(startLine, startColumn);
        }

        if (Peek() == '.' || IsNameStart(Peek()))
        {
            throw Error($"Invalid number, unexpected character '{Peek()}'.", startLine, startColumn);
        }

        return (_source[start.._position], isFloat ? TokenKind.Float : TokenKind.Int);
    }

    private void ReadDigits(int startLine, int startColumn)
    {
        if (!char.IsAsciiDigit(Peek()))
        {
            throw Error("Invalid number, expected a digit.", startLine, startColumn);
        }

        while (char.IsAsciiDigit(Peek()))
        {
            Read();
        }
    }

    private string ReadString(int startLine, int startColumn)
    {
        Read();
        StringBuilder builder = new();

        while (true)
        {
            if (IsEnd)
            {
                throw Error("Unterminated string literal.", startLine, startColumn);
            }

            char c = Read();

            if (c == '"')
            {
                return builder.ToString();
            }

            if (c is '\n' or '\r')
            {
                throw Error("Unterminated string literal.", startLine, startColumn);
            }

            if (c < ' ' && c != '\t')
            {
                throw Error($"Invalid character within string: U+{(int)c:X4}.", _line, _column - 1);
            }

            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            int escapeLine = _line;
            int escapeColumn = _column - 1;
            char escaped = Read();
            switch (escaped)
            {
                case '"': builder.Append('"'); break;
                case '\\': builder.Append('\\'); break;
                case '/': builder.Append('/'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'u': builder.Append(ReadUnicodeEscape(escapeLine, escapeColumn)); break;
                default:
                    throw Error($"Invalid character escape sequence: '\\{escaped}'.", escapeLine, escapeColumn);
            }
        }
    }

    private char ReadUnicodeEscape(int line, int column)
    {
        int code = 0;
        for (int i = 0; i < 4; i++)
        {
            char hex = Peek();
            if (!char.IsAsciiHexDigit(hex))
            {
                throw Error("Invalid Unicode escape sequence.", line, column);
            }

            code = code * 16 + Convert.ToInt32(hex.ToString(), 16);
            Read();
        }

        return (char)code;
    }

    private string ReadBlockString(int startLine, int startColumn)
    {
        Read();
        Read();
        Read(); // opening """
        StringBuilder raw = new();

        while (true)
        {
            if (IsEnd)
            {
                throw Error("Unterminated block string.", startLine, startColumn);
            }

            if (Peek() == '"' && PeekAt(1) == '"' && PeekAt(2) == '"')
            {
                Read();
                Read();
                Read();
                return DedentBlockString(raw.ToString());
            }

            if (Peek() == '\\' && PeekAt(1) == '"' && PeekAt(2) == '"' && PeekAt(3) == '"')
            {
                Read();
                Read();
                Read();
                Read();
                raw.Append("\"\"\"");
                continue;
            }

            raw.Append(Read());
        }
    }

    private static string DedentBlockString(string raw)
    {
        string[] lines = raw.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        int commonIndent = int.MaxValue;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            int indent = 0;
            while (indent < line.Length && line[indent] is ' ' or '\t')
            {
                indent++;
            }

            if (indent < line.Length && indent < commonIndent)
            {
                commonIndent = indent;
            }
        }

        if (commonIndent != int.MaxValue)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                lines[i] = lines[i].Length >= commonIndent ? lines[i][commonIndent..] : string.Empty;
            }
        }

        int first = 0;
        int last = lines.Length;
        while (first < last && string.IsNullOrWhiteSpace(lines[first]))
        {
            first++;
        }

        while (last > first && string.IsNullOrWhiteSpace(lines[last - 1]))
        {
            last--;
        }

        return string.Join('\n', lines[first..last]);
    }

    private Token CreateToken(string value, TokenKind tokenKind, int line, int column, int startPosition)
    {
        return new Token
        {
            Value = value,
            TokenKind = tokenKind,
            SourceLocation = new SourceLocation(line, column, startPosition, _position)
        };
    }
}
