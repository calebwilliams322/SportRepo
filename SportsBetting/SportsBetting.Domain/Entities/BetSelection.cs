using SportsBetting.Domain.Enums;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a specific selection within a bet
/// (Event + Market + Outcome + Locked Odds)
/// </summary>
public class BetSelection
{
    public Guid Id { get; }
    public Guid BetId { get; private set; }

    public Guid EventId { get; }
    public string EventName { get; }

    public Guid MarketId { get; }
    public MarketType MarketType { get; }
    public string MarketName { get; }

    public Guid OutcomeId { get; }
    public string OutcomeName { get; }

    /// <summary>
    /// The odds that were locked in when this selection was placed
    /// </summary>
    public Odds LockedOdds { get; }

    /// <summary>
    /// The line value (for spread/totals), if applicable
    /// </summary>
    public decimal? Line { get; }

    /// <summary>
    /// Result of this selection after settlement
    /// </summary>
    public SelectionResult Result { get; private set; }

    // Private parameterless constructor for EF Core
    private BetSelection()
    {
        EventName = string.Empty;
        MarketName = string.Empty;
        OutcomeName = string.Empty;
    }

    public BetSelection(
        Event evt,
        Market market,
        Outcome outcome)
        : this(evt, market, outcome, outcome.CurrentOdds)
    {
    }

    /// <summary>
    /// Creates a bet selection with custom odds (e.g., from LineLock)
    /// </summary>
    public BetSelection(
        Event evt,
        Market market,
        Outcome outcome,
        Odds lockedOdds)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));
        if (market == null)
            throw new ArgumentNullException(nameof(market));
        if (outcome == null)
            throw new ArgumentNullException(nameof(outcome));

        Id = Guid.NewGuid();
        EventId = evt.Id;
        EventName = evt.Name;
        MarketId = market.Id;
        MarketType = market.Type;
        MarketName = market.Name;
        OutcomeId = outcome.Id;
        OutcomeName = outcome.Name;
        LockedOdds = lockedOdds; // Use provided locked odds
        Line = outcome.Line;
        Result = SelectionResult.Pending;
    }

    internal void SetBetId(Guid betId)
    {
        BetId = betId;
    }

    /// <summary>
    /// Validates that this selection is attached to a bet
    /// </summary>
    private void EnsureAttachedToBet()
    {
        if (BetId == Guid.Empty)
            throw new InvalidOperationException(
                $"BetSelection for '{OutcomeName}' must be attached to a Bet before use. " +
                "This should not happen in normal usage - selections are created by Bet internally.");
    }

    /// <summary>
    /// Settle this selection based on the outcome result
    /// </summary>
    internal void Settle(Outcome outcome)
    {
        EnsureAttachedToBet();

        if (outcome.Id != OutcomeId)
            throw new InvalidOperationException("Outcome ID mismatch");

        if (outcome.IsVoid)
        {
            Result = SelectionResult.Void;
        }
        else if (outcome.IsWinner == true)
        {
            Result = SelectionResult.Won;
        }
        else if (outcome.IsWinner == false)
        {
            Result = SelectionResult.Lost;
        }
        else
        {
            // Push scenario (can happen with exact line hits in spread/totals)
            Result = SelectionResult.Pushed;
        }
    }

    /// <summary>
    /// Force void this selection
    /// </summary>
    internal void MarkAsVoid()
    {
        EnsureAttachedToBet();

        Result = SelectionResult.Void;
    }

    public bool IsWin => Result == SelectionResult.Won;
    public bool IsLoss => Result == SelectionResult.Lost;
    public bool IsPush => Result == SelectionResult.Pushed;
    public bool IsVoid => Result == SelectionResult.Void;
    public bool IsPending => Result == SelectionResult.Pending;

    public override string ToString()
    {
        var line = Line.HasValue ? $" {Line:+0.#;-0.#}" : "";
        return $"{EventName} - {OutcomeName}{line} @ {LockedOdds}";
    }
}
