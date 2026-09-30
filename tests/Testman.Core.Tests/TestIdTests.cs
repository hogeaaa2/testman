using Testman.Core.Specifications;
using System.Numerics;

namespace Testman.Core.Tests;

public sealed class TestIdTests
{
    [Theory]
    [InlineData("TC-1", 1)]
    [InlineData("TC-42", 42)]
    public void TryParse_accepts_valid_ids(string value, int expectedNumber)
    {
        var parsed = TestId.TryParse(value, out var testId);

        Assert.True(parsed);
        Assert.Equal(value, testId.Value);
        Assert.Equal(new BigInteger(expectedNumber), testId.Number);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TC-0")]
    [InlineData("TC-01")]
    [InlineData("tc-1")]
    [InlineData("TC-1 ")]
    [InlineData("T-1")]
    public void TryParse_rejects_values_outside_the_approved_format(string value)
    {
        var parsed = TestId.TryParse(value, out _);

        Assert.False(parsed);
    }
}
