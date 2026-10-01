namespace Taleem.Domain.Entities;

/// <summary>
/// Extension point for future daily/Ramadan/Friday recommendations.
/// Must reference published TaleemUnits; never stores generated religious quotations.
/// </summary>
public class CalendarRecommendationHook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HookType { get; set; } = string.Empty; // DailyTaleem, RamadanReading, FridayReading, IslamicCalendar
    public string? IslamicDateHint { get; set; }
    public Guid? PreferredTaleemUnitId { get; set; }
    public Guid? BookId { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
}
