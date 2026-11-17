using System.Globalization;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes.Interfaces;
using HazelnutQL.Core.HazelnutQL.Core.Parsing.Lexer;

namespace HazelnutQL.Core.HazelnutQL.Core.Parsing;

public class Parser
{
    public const int DefaultMaxDepth = 64;

    private readonly Lexer.Lexer _lexer;
    private readonly int _maxDepth;
    private Token _current;
    private int _lastEnd;
    private int _depth;

    public Parser(string source, int maxDepth = DefaultMaxDepth)
    {
        _lexer = new Lexer.Lexer(source);
        _maxDepth = maxDepth;
        _current = _lexer.NextToken();
    }

    public static DocumentNode Parse(string source, int maxDepth = DefaultMaxDepth)
    {
        return new Parser(source, maxDepth).ParseDocument();
    }

    public DocumentNode ParseDocument()
    {
        SourceLocation start = _current.SourceLocation;
        List<IDefinitionNode> definitions = [];

        do
        {
            definitions.Add(ParseDefinition());
        } while (_current.TokenKind != TokenKind.EndOfFile);

        return new DocumentNode { Definitions = definitions, Location = Span(start) };
    }

    private IDefinitionNode ParseDefinition()
    {
        if (_current.TokenKind == TokenKind.BraceL)
        {
            SourceLocation start = _current.SourceLocation;
            return new OperationDefinitionNode
            {
                Operation = OperationType.Query,
                SelectionSet = ParseSelectionSet(),
                Location = Span(start)
            };
        }

        if (_current.TokenKind == TokenKind.Name)
        {
            switch (_current.Value)
            {
                case "query":
                case "mutation":
                    return ParseOperationDefinition();
                case "fragment":
                    return ParseFragmentDefinition();
            }
        }

        throw Unexpected();
    }

    private OperationDefinitionNode ParseOperationDefinition()
    {
        SourceLocation start = _current.SourceLocation;
        OperationType operation = Advance().Value == "query" ? OperationType.Query : OperationType.Mutation;
        string? name = _current.TokenKind == TokenKind.Name ? Advance().Value : null;
        IReadOnlyList<VariableDefinitionNode> variables = ParseVariableDefinitions();
        IReadOnlyList<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode selectionSet = ParseSelectionSet();

        return new OperationDefinitionNode
        {
            Operation = operation,
            Name = name,
            VariableDefinitions = variables,
            Directives = directives,
            SelectionSet = selectionSet,
            Location = Span(start)
        };
    }

    private List<VariableDefinitionNode> ParseVariableDefinitions()
    {
        List<VariableDefinitionNode> variables = [];
        if (_current.TokenKind != TokenKind.ParenL)
        {
            return variables;
        }

        Advance();
        do
        {
            SourceLocation start = _current.SourceLocation;
            Expect(TokenKind.Dollar);
            string name = ExpectName();
            Expect(TokenKind.Colon);
            TypeNode type = ParseType();
            ValueNode? defaultValue = null;
            if (_current.TokenKind == TokenKind.Equals)
            {
                Advance();
                defaultValue = ParseValue(true);
            }

            variables.Add(new VariableDefinitionNode
            {
                Name = name,
                Type = type,
                DefaultValue = defaultValue,
                Location = Span(start)
            });
        } while (_current.TokenKind != TokenKind.ParenR);

        Expect(TokenKind.ParenR);
        return variables;
    }

    private TypeNode ParseType()
    {
        EnterNesting();
        SourceLocation start = _current.SourceLocation;
        TypeNode type;

        if (_current.TokenKind == TokenKind.BracketL)
        {
            Advance();
            TypeNode element = ParseType();
            Expect(TokenKind.BracketR);
            type = new ListTypeNode { ElementType = element, Location = Span(start) };
        }
        else
        {
            type = new NamedTypeNode { Name = ExpectName(), Location = Span(start) };
        }

        if (_current.TokenKind == TokenKind.Bang)
        {
            Advance();
            type = new NonNullTypeNode { InnerType = type, Location = Span(start) };
        }

        ExitNesting();
        return type;
    }

    private FragmentDefinitionNode ParseFragmentDefinition()
    {
        SourceLocation start = _current.SourceLocation;
        Advance(); // 'fragment'

        if (_current.TokenKind == TokenKind.Name && _current.Value == "on")
        {
            throw Unexpected();
        }

        string name = ExpectName();
        ExpectKeyword("on");
        NamedTypeNode typeCondition = ParseNamedType();
        IReadOnlyList<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode selectionSet = ParseSelectionSet();

        return new FragmentDefinitionNode
        {
            Name = name,
            TypeCondition = typeCondition,
            Directives = directives,
            SelectionSet = selectionSet,
            Location = Span(start)
        };
    }

    private NamedTypeNode ParseNamedType()
    {
        SourceLocation start = _current.SourceLocation;
        return new NamedTypeNode { Name = ExpectName(), Location = Span(start) };
    }

    private SelectionSetNode ParseSelectionSet()
    {
        EnterNesting();
        SourceLocation start = _current.SourceLocation;
        Expect(TokenKind.BraceL);

        List<ISelectionNode> selections = [];
        do
        {
            selections.Add(ParseSelection());
        } while (_current.TokenKind != TokenKind.BraceR);

        Expect(TokenKind.BraceR);
        ExitNesting();
        return new SelectionSetNode { Selections = selections, Location = Span(start) };
    }

    private ISelectionNode ParseSelection()
    {
        return _current.TokenKind == TokenKind.Spread ? ParseFragment() : ParseField();
    }

    private ISelectionNode ParseFragment()
    {
        SourceLocation start = _current.SourceLocation;
        Expect(TokenKind.Spread);

        bool isSpread = _current.TokenKind == TokenKind.Name && _current.Value != "on";
        if (isSpread)
        {
            string name = Advance().Value;
            return new FragmentSpreadNode
            {
                Name = name,
                Directives = ParseDirectives(false),
                Location = Span(start)
            };
        }

        NamedTypeNode? typeCondition = null;
        if (_current.TokenKind == TokenKind.Name)
        {
            Advance(); // 'on'
            typeCondition = ParseNamedType();
        }

        IReadOnlyList<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode selectionSet = ParseSelectionSet();
        return new InlineFragmentNode
        {
            TypeCondition = typeCondition,
            Directives = directives,
            SelectionSet = selectionSet,
            Location = Span(start)
        };
    }

    private FieldNode ParseField()
    {
        SourceLocation start = _current.SourceLocation;
        string name = ExpectName();
        string? alias = null;

        if (_current.TokenKind == TokenKind.Colon)
        {
            Advance();
            alias = name;
            name = ExpectName();
        }

        IReadOnlyList<ArgumentNode> arguments = ParseArguments(false);
        IReadOnlyList<DirectiveNode> directives = ParseDirectives(false);
        SelectionSetNode? selectionSet = _current.TokenKind == TokenKind.BraceL ? ParseSelectionSet() : null;

        return new FieldNode
        {
            Alias = alias,
            Name = name,
            Arguments = arguments,
            Directives = directives,
            SelectionSet = selectionSet,
            Location = Span(start)
        };
    }

    private List<ArgumentNode> ParseArguments(bool isConst)
    {
        List<ArgumentNode> arguments = [];
        if (_current.TokenKind != TokenKind.ParenL)
        {
            return arguments;
        }

        Advance();
        do
        {
            SourceLocation start = _current.SourceLocation;
            string name = ExpectName();
            Expect(TokenKind.Colon);
            arguments.Add(new ArgumentNode { Name = name, Value = ParseValue(isConst), Location = Span(start) });
        } while (_current.TokenKind != TokenKind.ParenR);

        Expect(TokenKind.ParenR);
        return arguments;
    }

    private List<DirectiveNode> ParseDirectives(bool isConst)
    {
        List<DirectiveNode> directives = [];
        while (_current.TokenKind == TokenKind.At)
        {
            SourceLocation start = _current.SourceLocation;
            Advance();
            string name = ExpectName();
            directives.Add(new DirectiveNode
            {
                Name = name,
                Arguments = ParseArguments(isConst),
                Location = Span(start)
            });
        }

        return directives;
    }

    private ValueNode ParseValue(bool isConst)
    {
        SourceLocation start = _current.SourceLocation;

        switch (_current.TokenKind)
        {
            case TokenKind.Dollar:
                if (isConst)
                {
                    throw Error("Unexpected variable in a constant value.", start);
                }

                Advance();
                return new VariableNode { Name = ExpectName(), Location = Span(start) };

            case TokenKind.Int:
            {
                Token token = Advance();
                if (!long.TryParse(token.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
                {
                    throw Error($"Int value '{token.Value}' is out of range.", start);
                }

                return new IntValueNode { Value = value, Location = Span(start) };
            }

            case TokenKind.Float:
            {
                Token token = Advance();
                double value = double.Parse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (!double.IsFinite(value))
                {
                    throw Error($"Float value '{token.Value}' is out of range.", start);
                }

                return new FloatValueNode { Value = value, Location = Span(start) };
            }

            case TokenKind.String:
                return new StringValueNode { Value = Advance().Value, Location = Span(start) };

            case TokenKind.Boolean:
                return new BooleanValueNode { Value = Advance().Value == "true", Location = Span(start) };

            case TokenKind.Null:
                Advance();
                return new NullValueNode { Location = Span(start) };

            case TokenKind.Name:
                return new EnumValueNode { Value = Advance().Value, Location = Span(start) };

            case TokenKind.BracketL:
                return ParseListValue(isConst);

            case TokenKind.BraceL:
                return ParseObjectValue(isConst);

            default:
                throw Unexpected();
        }
    }

    private ListValueNode ParseListValue(bool isConst)
    {
        EnterNesting();
        SourceLocation start = _current.SourceLocation;
        Advance();

        List<ValueNode> values = [];
        while (_current.TokenKind != TokenKind.BracketR)
        {
            values.Add(ParseValue(isConst));
        }

        Advance();
        ExitNesting();
        return new ListValueNode { Values = values, Location = Span(start) };
    }

    private ObjectValueNode ParseObjectValue(bool isConst)
    {
        EnterNesting();
        SourceLocation start = _current.SourceLocation;
        Advance();

        List<ObjectFieldNode> fields = [];
        while (_current.TokenKind != TokenKind.BraceR)
        {
            SourceLocation fieldStart = _current.SourceLocation;
            string name = ExpectName();
            Expect(TokenKind.Colon);
            fields.Add(new ObjectFieldNode { Name = name, Value = ParseValue(isConst), Location = Span(fieldStart) });
        }

        Advance();
        ExitNesting();
        return new ObjectValueNode { Fields = fields, Location = Span(start) };
    }

    private Token Advance()
    {
        Token previous = _current;
        _lastEnd = previous.SourceLocation.End;
        _current = _lexer.NextToken();
        return previous;
    }

    private Token Expect(TokenKind kind)
    {
        if (_current.TokenKind != kind)
        {
            throw Error($"Expected {kind}, found {_current}.", _current.SourceLocation);
        }

        return Advance();
    }

    private string ExpectName()
    {
        return Expect(TokenKind.Name).Value;
    }

    private void ExpectKeyword(string keyword)
    {
        if (_current.TokenKind != TokenKind.Name || _current.Value != keyword)
        {
            throw Error($"Expected '{keyword}', found {_current}.", _current.SourceLocation);
        }

        Advance();
    }

    private void EnterNesting()
    {
        if (++_depth > _maxDepth)
        {
            throw Error($"Maximum nesting depth of {_maxDepth} exceeded.", _current.SourceLocation);
        }
    }

    private void ExitNesting()
    {
        _depth--;
    }

    private SourceLocation Span(SourceLocation start)
    {
        return start with { End = _lastEnd };
    }

    private GraphQLSyntaxException Unexpected()
    {
        return Error($"Unexpected {_current}.", _current.SourceLocation);
    }

    private static GraphQLSyntaxException Error(string message, SourceLocation location)
    {
        return new GraphQLSyntaxException(message, location);
    }
}
