using System.Globalization;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.Worker.Services;

/// <summary>
/// The Worker's risk step (guide §27.8). Reads the thresholds, loads every active enrollment's inputs in one query,
/// and adds a RiskFlags snapshot only when the values differ from the latest one (spec Q1), in one save.
/// </summary>
public class RiskCalculator(IRiskRepository risks, ISystemSettingRepository settings)
{
    public async Task<(int Enrollments, int NewSnapshots, int AtRisk)> RecomputeAsync(DateTime nowUtc, CancellationToken ct)
    {
        // A bad setting throws before anything is read or written; Hangfire logs the failed tick and the next one retries.
        var threshold = await ReadDecimalAsync("AbsenceThreshold", ct);
        var passing = await ReadDecimalAsync("PassingGrade", ct);

        var inputs = await risks.ListInputsAsync(ct);
        int added = 0, atRisk = 0;
        foreach (var input in inputs)
        {
            var (rate, average, isAtRisk) = RiskRules.Compute(
                input.AbsentCount, input.CountedCount, input.WeightedSum, input.WeightSum, threshold, passing);
            if (isAtRisk) atRisk++;
            if (input.Latest == new RiskSnapshot(rate, average, isAtRisk)) continue; // unchanged: no new history row

            risks.Add(new RiskFlag
            {
                EnrollmentId = input.EnrollmentId,
                AbsenceRate = rate,
                WeightedAverage = average,
                IsAtRisk = isAtRisk,
                ComputedAt = nowUtc
            });
            added++;
        }

        await risks.SaveChangesAsync(ct);
        return (inputs.Count, added, atRisk);
    }

    private async Task<decimal> ReadDecimalAsync(string key, CancellationToken ct) =>
        decimal.TryParse(await settings.GetValueAsync(key, ct), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"Setting '{key}' is missing or not a number.");
}
