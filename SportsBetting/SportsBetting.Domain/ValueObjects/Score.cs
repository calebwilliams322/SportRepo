namespace SportsBetting.Domain.ValueObjects;

/// <summary>
/// Represents the score of a sporting event
/// </summary>
public readonly struct Score : IEquatable<Score>
{
    public int HomeScore { get; }
    public int AwayScore { get; }

    public Score(int homeScore, int awayScore)
    {
        if (homeScore < 0)
            throw new ArgumentException("Home score cannot be negative", nameof(homeScore));
        if (awayScore < 0)
            throw new ArgumentException("Away score cannot be negative", nameof(awayScore));

        HomeScore = homeScore;
        AwayScore = awayScore;
    }

    public int TotalPoints => HomeScore + AwayScore;

    public int Margin => Math.Abs(HomeScore - AwayScore);

    public bool IsHomeWin => HomeScore > AwayScore;

    public bool IsAwayWin => AwayScore > HomeScore;

    public bool IsDraw => HomeScore == AwayScore;

    public bool Equals(Score other)
    {
        return HomeScore == other.HomeScore && AwayScore == other.AwayScore;
    }

    public override bool Equals(object? obj)
    {
        return obj is Score other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HomeScore, AwayScore);
    }

    public static bool operator ==(Score left, Score right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Score left, Score right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{HomeScore}-{AwayScore}";
    }
}
