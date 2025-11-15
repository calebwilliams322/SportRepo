using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a specific outcome within a betting market
/// (e.g., "Team A wins", "Over 210.5", "Team B -5.5")
/// </summary>
public class Outcome
{
    public Guid Id { get; }
    public Guid MarketId { get; private set; }
    public string Name { get; }
    public string Description { get; }

    /// <summary>
    /// Current odds for this outcome (in bookmaker model)
    /// In P2P exchange, this would be derived from best available offers
    /// </summary>
    public Odds CurrentOdds { get; private set; }

    /// <summary>
    /// For outcomes with a line (e.g., spread, totals)
    /// </summary>
    public decimal? Line { get; }

    /// <summary>
    /// Whether this outcome won (set during settlement)
    /// </summary>
    public bool? IsWinner { get; private set; }

    /// <summary>
    /// Whether this outcome was voided
    /// </summary>
    public bool IsVoid { get; private set; }

    // Private parameterless constructor for EF Core
    private Outcome()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        Description = string.Empty;
        // Initialize value objects
        CurrentOdds = new Odds(1.0m);
    }

    public Outcome(string name, string description, Odds initialOdds, decimal? line = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Outcome name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        Description = description ?? name;
        CurrentOdds = initialOdds;
        Line = line;
        IsWinner = null;
        IsVoid = false;
    }

    internal void SetMarketId(Guid marketId)
    {
        MarketId = marketId;
    }

    /// <summary>
    /// Validates that this outcome is attached to a market
    /// </summary>
    private void EnsureAttachedToMarket()
    {
        if (MarketId == Guid.Empty)
            throw new InvalidOperationException(
                $"Outcome '{Name}' must be attached to a Market before use. " +
                "Use market.AddOutcome(outcome) to properly attach it.");
    }

    /// <summary>
    /// Update the current odds (for bookmaker model)
    /// </summary>
    public void UpdateOdds(Odds newOdds)
    {
        CurrentOdds = newOdds;
    }

    /// <summary>
    /// Mark this outcome as the winner during settlement
    /// </summary>
    internal void MarkAsWinner()
    {
        EnsureAttachedToMarket();

        if (IsVoid)
            throw new InvalidOperationException("Cannot mark a void outcome as winner");

        IsWinner = true;
    }

    /// <summary>
    /// Mark this outcome as a loser during settlement
    /// </summary>
    internal void MarkAsLoser()
    {
        EnsureAttachedToMarket();

        if (IsVoid)
            throw new InvalidOperationException("Cannot mark a void outcome as loser");

        IsWinner = false;
    }

    /// <summary>
    /// Void this outcome
    /// </summary>
    internal void MarkAsVoid()
    {
        EnsureAttachedToMarket();

        IsVoid = true;
        IsWinner = null;
    }

    /// <summary>
    /// Mark this outcome as pushed (tie on the line)
    /// IsWinner remains null, but IsVoid is false
    /// </summary>
    internal void MarkAsPush()
    {
        EnsureAttachedToMarket();

        IsWinner = null;
        IsVoid = false;
    }

    public override string ToString()
    {
        if (Line.HasValue)
            return $"{Name} {Line:+0.#;-0.#} @ {CurrentOdds}";
        return $"{Name} @ {CurrentOdds}";
    }
}
