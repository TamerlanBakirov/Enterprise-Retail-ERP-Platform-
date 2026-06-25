namespace GeorgiaERP.Infrastructure.Reporting;

public static class SimpleCronParser
{
    public static DateTimeOffset? GetNextOccurrence(string cronExpression, DateTimeOffset from)
    {
        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return null;

        if (!TryParsePart(parts[0], out var minute) ||
            !TryParsePart(parts[1], out var hour))
            return null;

        TryParsePart(parts[2], out var dayOfMonth);
        TryParsePart(parts[4], out var dayOfWeek);

        var candidate = new DateTimeOffset(
            from.Year, from.Month, from.Day, hour ?? 0, minute ?? 0, 0, from.Offset)
            .AddMinutes(1);

        if (candidate <= from)
            candidate = candidate.AddDays(1);

        for (var i = 0; i < 366; i++)
        {
            var dt = candidate.AddDays(i);
            var d = new DateTimeOffset(dt.Year, dt.Month, dt.Day, hour ?? 0, minute ?? 0, 0, dt.Offset);

            if (d <= from) continue;
            if (dayOfMonth.HasValue && d.Day != dayOfMonth.Value) continue;
            if (dayOfWeek.HasValue && (int)d.DayOfWeek != dayOfWeek.Value) continue;

            return d;
        }

        return null;
    }

    private static bool TryParsePart(string part, out int? value)
    {
        value = null;
        if (part == "*") return true;
        if (int.TryParse(part, out var v))
        {
            value = v;
            return true;
        }
        return false;
    }
}
