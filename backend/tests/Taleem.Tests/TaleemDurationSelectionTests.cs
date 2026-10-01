using Taleem.Application.Common;
using Taleem.Domain.Entities;

namespace Taleem.Tests;

public class TaleemDurationSelectionTests
{
    [Fact]
    public void Prefers_Complete_Units_Within_Budget()
    {
        var units = new List<TaleemUnit>
        {
            new() { Title = "A", EstimatedReadingSeconds = 180, OrderIndex = 0 },
            new() { Title = "B", EstimatedReadingSeconds = 240, OrderIndex = 1 },
            new() { Title = "C", EstimatedReadingSeconds = 600, OrderIndex = 2 },
        };

        var budget = 10 * 60;
        var selected = new List<TaleemUnit>();
        var total = 0;
        foreach (var unit in units)
        {
            if (selected.Count == 0 || total + unit.EstimatedReadingSeconds <= budget + 45)
            {
                selected.Add(unit);
                total += unit.EstimatedReadingSeconds;
            }
            if (total >= budget) break;
        }

        Assert.Equal(2, selected.Count);
        Assert.DoesNotContain(selected, u => u.Title == "C");
        Assert.True(total <= budget + 45);
    }

    [Fact]
    public void SlugHelper_Is_Stable()
    {
        Assert.Equal("fazail-e-amaal", SlugHelper.Slugify("Fazail-e-Amaal"));
    }
}
