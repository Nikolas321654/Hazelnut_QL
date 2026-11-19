namespace HazelnutQL.Core.HazelnutQL.Core.Parsing.AST_Nodes;

public abstract class ValueNode : ASTNode;

public sealed class VariableNode : ValueNode
{
    public required string Name { get; init; }
}

public sealed class IntValueNode : ValueNode
{
    public required string Value { get; init; }
}

public sealed class FloatValueNode : ValueNode
{
    public required double Value { get; init; }
}

public sealed class StringValueNode : ValueNode
{
    public required string Value { get; init; }
}

public sealed class BooleanValueNode : ValueNode
{
    public required bool Value { get; init; }
}

public sealed class NullValueNode : ValueNode;

public sealed class EnumValueNode : ValueNode
{
    public required string Value { get; init; }
}

public sealed class ListValueNode : ValueNode
{
    public IReadOnlyList<ValueNode> Values { get; init; } = [];
}

public sealed class ObjectFieldNode : ASTNode
{
    public required string Name { get; init; }
    public required ValueNode Value { get; init; }
}

public sealed class ObjectValueNode : ValueNode
{
    public IReadOnlyList<ObjectFieldNode> Fields { get; init; } = [];
}
