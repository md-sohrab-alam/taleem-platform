namespace Taleem.Application.Common;

public static class ReadingTimeCalculator
{
    /// <summary>Approximate adult reading speed for mixed religious text.</summary>
    public const int WordsPerMinute = 160;

    public static int EstimateSeconds(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 30;

        var words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        var seconds = (int)Math.Ceiling(words / (double)WordsPerMinute * 60);
        return Math.Clamp(seconds, 30, 60 * 60);
    }
}
