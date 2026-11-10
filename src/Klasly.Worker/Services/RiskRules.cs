namespace Klasly.Worker.Services;

/// <summary>
/// The at-risk rule (guide §27.8) with no database, so the boundaries are plain unit tests.
/// Values are rounded to the RiskFlags column scales first, and the flag is decided on those rounded values,
/// so a dashboard never shows "75.00" next to an at-risk flag.
/// </summary>
public static class RiskRules
{
    /// <param name="absent">The student's Absent records in this class's Finalized sessions.</param>
    /// <param name="counted">The student's Present + Late + Absent records there (Excused left out).</param>
    /// <param name="weightedSum">Σ(score ÷ max × 100 × weight) over the student's graded assessments.</param>
    /// <param name="weightSum">Σ(weight) over the same assessments; 0 when nothing is graded yet.</param>
    public static (decimal Rate, decimal? Average, bool AtRisk) Compute(
        int absent, int counted, decimal weightedSum, decimal weightSum, decimal threshold, decimal passing)
    {
        var rate = counted == 0 ? 0m : Math.Round((decimal)absent / counted, 4, MidpointRounding.AwayFromZero);
        decimal? average = weightSum == 0 ? null : Math.Round(weightedSum / weightSum, 2, MidpointRounding.AwayFromZero);
        return (rate, average, rate >= threshold || average < passing); // null < passing is false: no grades, no grade flag
    }
}
