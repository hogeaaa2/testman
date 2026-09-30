using System.Globalization;
using System.Numerics;

namespace Testman.Core.Specifications;

public sealed record TestId
{
    private const string Prefix = "TC-";

    private TestId(string value, BigInteger number)
    {
        Value = value;
        Number = number;
    }

    public string Value { get; }

    public BigInteger Number { get; }

    public static bool TryParse(string value, out TestId testId)
    {
        testId = null!;

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var numberText = value.AsSpan(Prefix.Length);
        if (numberText.IsEmpty || numberText[0] == '0')
        {
            return false;
        }

        foreach (var character in numberText)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        if (!BigInteger.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        testId = new TestId(value, number);
        return true;
    }

    public override string ToString() => Value;
}
