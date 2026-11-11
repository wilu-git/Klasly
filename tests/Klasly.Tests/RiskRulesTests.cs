using Klasly.Worker.Services;

namespace Klasly.Tests;

/// <summary>The at-risk rule on its own (guide §27.8): the boundary cases §27.10 asks for, with no database.</summary>
public class RiskRulesTests
{
    private const decimal Threshold = 0.20m;
    private const decimal Passing = 75m;

    [Theory]
    [InlineData(1, 5, 0.2000, true)]  // exactly 20% → at-risk
    [InlineData(0, 5, 0.0000, false)]
    [InlineData(1, 3, 0.3333, true)]
    [InlineData(0, 0, 0.0000, false)] // no finalized sessions yet
    public void Absence_rate_is_rounded_to_4_decimals_and_20_percent_is_at_risk(int absent, int counted, decimal rate, bool atRisk)
    {
        var result = RiskRules.Compute(absent, counted, weightedSum: 0, weightSum: 0, Threshold, Passing);

        Assert.Equal(rate, result.Rate);
        Assert.Null(result.Average);
        Assert.Equal(atRisk, result.AtRisk);
    }

    [Fact]
    public void Rate_just_under_the_threshold_is_not_at_risk()
    {
        // 1999 of 10000 → 0.1999
        Assert.False(RiskRules.Compute(1999, 10000, 0, 0, Threshold, Passing).AtRisk);
    }

    [Theory]
    [InlineData(7500, 100, 75.00, false)]  // exactly 75 → not at-risk
    [InlineData(7499, 100, 74.99, true)]
    [InlineData(74996, 1000, 75.00, false)] // 74.996 shows as 75.00, so the flag agrees with what is shown
    [InlineData(3900, 50, 78.00, false)]   // 18/20 × 30% + 30/50 × 20%
    public void Average_is_rounded_to_2_decimals_and_exactly_75_is_not_at_risk(
        decimal weightedSum, decimal weightSum, decimal average, bool atRisk)
    {
        var result = RiskRules.Compute(absent: 0, counted: 5, weightedSum, weightSum, Threshold, Passing);

        Assert.Equal(average, result.Average);
        Assert.Equal(atRisk, result.AtRisk);
    }

    [Fact]
    public void Either_condition_alone_flags_the_student()
    {
        Assert.True(RiskRules.Compute(1, 5, 9000, 100, Threshold, Passing).AtRisk); // good grades, too many absences
        Assert.True(RiskRules.Compute(0, 5, 5000, 100, Threshold, Passing).AtRisk); // perfect attendance, failing average
    }
}
