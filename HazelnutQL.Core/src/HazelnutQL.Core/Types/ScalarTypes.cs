using System.Globalization;
using System.Text.Json;

namespace HazelnutQL.Core.HazelnutQL.Core.Schema;

public static class ScalarTypes
{
    public static readonly ScalarType Int = new("Int", ParseInt, ParseInt);
    public static readonly ScalarType Float = new("Float", ParseFloat, ParseFloat);
    public static readonly ScalarType String = new("String", ParseString, ParseString);
    public static readonly ScalarType Boolean = new("Boolean", ParseBoolean, ParseBoolean);
    public static readonly ScalarType ID = new("ID", ParseId, ParseId);

    public static IReadOnlyList<ScalarType> All { get; } = [Int, Float, String, Boolean, ID];

    private static object Unwrap(object value)
    {
        if (value is not JsonElement e)
        {
            return value;
        }

        return e.ValueKind switch
        {
            JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
            JsonValueKind.String => e.GetString()!,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new FormatException($"Unsupported JSON value kind '{e.ValueKind}'.")
        };
    }

    private static object? ParseInt(object? raw)
    {
        switch (Unwrap(raw!))
        {
            case int i:
                return i;
            case long l when l is >= int.MinValue and <= int.MaxValue:
                return (int)l;
            case short s:
                return (int)s;
            case byte b:
                return (int)b;
            case double d when d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue:
                return (int)d;
            default:
                throw new FormatException($"Int cannot represent value: {raw}");
        }
    }

    private static object? ParseFloat(object? raw)
    {
        switch (Unwrap(raw!))
        {
            case double d when double.IsFinite(d):
                return d;
            case float f when float.IsFinite(f):
                return (double)f;
            case int i:
                return (double)i;
            case long l:
                return (double)l;
            default:
                throw new FormatException($"Float cannot represent value: {raw}");
        }
    }

    private static object? ParseString(object? raw)
    {
        return Unwrap(raw!) switch
        {
            string s => s,
            _ => throw new FormatException($"String cannot represent value: {raw}")
        };
    }

    private static object? ParseBoolean(object? raw)
    {
        return Unwrap(raw!) switch
        {
            bool b => b,
            _ => throw new FormatException($"Boolean cannot represent value: {raw}")
        };
    }

    private static object? ParseId(object? raw)
    {
        return Unwrap(raw!) switch
        {
            string s => s,
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => throw new FormatException($"ID cannot represent value: {raw}")
        };
    }
}