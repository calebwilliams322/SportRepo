namespace SportsBetting.Domain.ValueObjects;

/// <summary>
/// Represents betting odds with payout calculation
/// Stores odds as decimal format internally (e.g., 2.5 means $2.50 returned per $1 wagered)
/// </summary>
public readonly struct Odds : IEquatable<Odds>
{
    /// <summary>
    /// Decimal odds (e.g., 2.5 = $250 payout on $100 stake including stake)
    /// </summary>
    public decimal DecimalValue { get; }

    public Odds(decimal decimalValue)
    {
        if (decimalValue < 1.0m)
            throw new ArgumentException("Decimal odds must be at least 1.0", nameof(decimalValue));

        DecimalValue = decimalValue;
    }

    /// <summary>
    /// Create odds from American format
    /// </summary>
    /// <param name="americanOdds">American odds (e.g., -150, +200)</param>
    public static Odds FromAmerican(int americanOdds)
    {
        if (americanOdds == 0)
            throw new ArgumentException("American odds cannot be zero");

        decimal decimalOdds;
        if (americanOdds > 0)
        {
            // Positive American odds: +200 = 3.0 decimal
            decimalOdds = (americanOdds / 100m) + 1;
        }
        else
        {
            // Negative American odds: -150 = 1.67 decimal
            decimalOdds = (100m / Math.Abs(americanOdds)) + 1;
        }

        return new Odds(decimalOdds);
    }

    /// <summary>
    /// Convert to American odds format
    /// </summary>
    public int ToAmerican()
    {
        if (DecimalValue >= 2.0m)
        {
            // Positive American odds
            return (int)Math.Round((DecimalValue - 1) * 100);
        }
        else
        {
            // Negative American odds
            return (int)Math.Round(-100 / (DecimalValue - 1));
        }
    }

    /// <summary>
    /// Calculate total payout (including stake) for a given stake amount
    /// </summary>
    public Money CalculatePayout(Money stake)
    {
        return new Money(stake.Amount * DecimalValue, stake.Currency);
    }

    /// <summary>
    /// Calculate profit only (excluding stake) for a given stake amount
    /// </summary>
    public Money CalculateProfit(Money stake)
    {
        return new Money(stake.Amount * (DecimalValue - 1), stake.Currency);
    }

    /// <summary>
    /// Multiply odds together (for parlays)
    /// </summary>
    public static Odds operator *(Odds a, Odds b)
    {
        return new Odds(a.DecimalValue * b.DecimalValue);
    }

    public bool Equals(Odds other)
    {
        return DecimalValue == other.DecimalValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is Odds other && Equals(other);
    }

    public override int GetHashCode()
    {
        return DecimalValue.GetHashCode();
    }

    public static bool operator ==(Odds left, Odds right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Odds left, Odds right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return DecimalValue.ToString("F2");
    }
}
