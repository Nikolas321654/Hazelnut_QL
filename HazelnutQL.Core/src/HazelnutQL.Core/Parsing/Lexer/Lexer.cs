namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public class Lexer(string source)
{
    private int _column = 1;
    private int _line = 1;
    private int _position = 0;
    private readonly string _source = source ?? string.Empty;

    private char Read()
    {
        if (_position >= _source.Length)
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
        return _position >= _source.Length ? '\0' : _source[_position];
    }

    private char PeekAt(int offset)
    {
        int index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private void SkipIgnored()
    {
        while (_position < _source.Length)
        {
            char c = Peek();

            if (c is ' ' or '\t' or '\r' or '\n' or ',')
            {
                Read();
                continue;
            }

            if (c == '#')
            {
                while (Peek() != '\0' && Peek() != '\n' && Peek() != '\r')
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

        char currentChar = Peek();

        if (currentChar == '\0')
        {
            return new Token
            {
                Value = string.Empty,
                TokenKind = TokenKind.EndOfFile,
                SourceLocation = new SourceLocation(startLine, startColumn, startPosition, startPosition)
            };
        }

        switch (currentChar)
        {
            case '!':
                Read();
                return CreateToken("!", TokenKind.Bang, startLine, startColumn, startPosition);
            case '$':
                Read();
                return CreateToken("$", TokenKind.Dollar, startLine, startColumn, startPosition);
            case '&':
                Read();
                return CreateToken("&", TokenKind.Ampersand, startLine, startColumn, startPosition);
            case '(':
                Read();
                return CreateToken("(", TokenKind.ParenL, startLine, startColumn, startPosition);
            case ')':
                Read();
                return CreateToken(")", TokenKind.ParenR, startLine, startColumn, startPosition);
            case ':':
                Read();
                return CreateToken(":", TokenKind.Colon, startLine, startColumn, startPosition);
            case '=':
                Read();
                return CreateToken("=", TokenKind.Equals, startLine, startColumn, startPosition);
            case '@':
                Read();
                return CreateToken("@", TokenKind.At, startLine, startColumn, startPosition);
            case '[':
                Read();
                return CreateToken("[", TokenKind.BracketL, startLine, startColumn, startPosition);
            case ']':
                Read();
                return CreateToken("]", TokenKind.BracketR, startLine, startColumn, startPosition);
            case '{':
                Read();
                return CreateToken("{", TokenKind.BraceL, startLine, startColumn, startPosition);
            case '|':
                Read();
                return CreateToken("|", TokenKind.Pipe, startLine, startColumn, startPosition);
            case '}':
                Read();
                return CreateToken("}", TokenKind.BraceR, startLine, startColumn, startPosition);
            case '.':
                if (PeekAt(1) == '.' && PeekAt(2) == '.')
                {
                    Read();
                    Read();
                    Read();
                    return CreateToken("...", TokenKind.Spread, startLine, startColumn, startPosition);
                }

                break;
        }

        if (currentChar == '"')
        {
            string value = ReadString();
            return CreateToken(value, TokenKind.String, startLine, startColumn, startPosition);
        }

        if (char.IsAsciiDigit(currentChar) || (currentChar == '-' && char.IsAsciiDigit(PeekAt(1))))
        {
            (string value, TokenKind kind) = ReadNumber();
            return CreateToken(value, kind, startLine, startColumn, startPosition);
        }

        if (currentChar == '_' || char.IsAsciiLetter(currentChar))
        {
            string value = ReadName();
            return CreateToken(value, TokenKind.Name, startLine, startColumn, startPosition);
        }

        char unknown = Read();
        throw new FormatException($"Unexpected character '{unknown}' at line {startLine}, column {startColumn}.");
    }

    private string ReadName()
    {
        int start = _position;
        while (_position < _source.Length)
        {
            char current = Peek();
            if (current == '_' || char.IsAsciiLetterOrDigit(current))
            {
                Read();
            }
            else
            {
                break;
            }
        }

        return _source[start.._position];
    }

    private (string Value, TokenKind Kind) ReadNumber()
    {
        int start = _position;
        bool isFloat = false;

        if (Peek() == '-')
        {
            Read();
        }

        while (char.IsAsciiDigit(Peek()))
        {
            Read();
        }

        if (Peek() == '.' && char.IsAsciiDigit(PeekAt(1)))
        {
            isFloat = true;
            Read(); // '.'

            while (char.IsAsciiDigit(Peek()))
            {
                Read();
            }
        }

        if (Peek() is 'e' or 'E')
        {
            isFloat = true;
            Read();

            if (Peek() is '+' or '-')
            {
                Read();
            }

            while (char.IsAsciiDigit(Peek()))
            {
                Read();
            }
        }

        string number = _source[start.._position];
        return (number, isFloat ? TokenKind.Float : TokenKind.Int);
    }

    private string ReadString()
    {
        Read();
        int start = _position;

        while (Peek() != '\0' && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Read();
            }

            Read();
        }

        if (Peek() == '\0')
        {
            throw new FormatException("Unterminated string literal.");
        }

        string str = _source[start.._position];
        Read();

        return str;
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
