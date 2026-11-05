namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

public readonly record struct SourceLocation(int Line, int Column, int Start, int End);
