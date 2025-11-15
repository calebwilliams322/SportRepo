using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a betting market within an event
/// (e.g., Moneyline, Spread, Totals)
/// </summary>
public class Market
{
    public Guid Id { get; }
    public Guid EventId { get; private set; }
    public MarketType Type { get; }
    public string Name { get; }
    public string? Description { get; }

    private readonly List<Outcome> _outcomes;
    public IReadOnlyList<Outcome> Outcomes => _outcomes.AsReadOnly();

    /// <summary>
    /// Whether the market is currently open for betting
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Whether the market has been settled
    /// </summary>
    public bool IsSettled { get; private set; }

    // Private parameterless constructor for EF Core
    private Market()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        // Initialize collections
        _outcomes = new List<Outcome>();
    }

    public Market(MarketType type, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Market name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Type = type;
        Name = name;
        Description = description;
        _outcomes = new List<Outcome>();
        IsOpen = true;
        IsSettled = false;
    }

    internal void SetEventId(Guid eventId)
    {
        EventId = eventId;
    }

    /// <summary>
    /// Validates that this market is attached to an event
    /// </summary>
    private void EnsureAttachedToEvent()
    {
        if (EventId == Guid.Empty)
            throw new InvalidOperationException(
                $"Market '{Name}' must be attached to an Event before use. " +
                "Use event.AddMarket(market) to properly attach it.");
    }

    /// <summary>
    /// Add an outcome to this market
    /// </summary>
    public void AddOutcome(Outcome outcome)
    {
        if (outcome == null)
            throw new ArgumentNullException(nameof(outcome));

        if (_outcomes.Any(o => o.Name == outcome.Name))
            throw new InvalidOperationException($"Outcome with name {outcome.Name} already exists in this market");

        outcome.SetMarketId(Id);
        _outcomes.Add(outcome);
    }

    /// <summary>
    /// Close the market (no more bets accepted)
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Suspend the market temporarily
    /// </summary>
    public void Suspend()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Reopen the market
    /// </summary>
    public void Reopen()
    {
        if (IsSettled)
            throw new InvalidOperationException("Cannot reopen a settled market");

        IsOpen = true;
    }

    /// <summary>
    /// Settle the market by marking winning outcomes
    /// </summary>
    public void Settle(params Guid[] winningOutcomeIds)
    {
        EnsureAttachedToEvent();

        if (IsSettled)
            throw new SettlementException("Market is already settled");

        if (winningOutcomeIds == null || winningOutcomeIds.Length == 0)
            throw new ArgumentException("Must specify at least one winning outcome", nameof(winningOutcomeIds));

        var winningOutcomes = new HashSet<Guid>(winningOutcomeIds);

        foreach (var outcome in _outcomes)
        {
            if (winningOutcomes.Contains(outcome.Id))
            {
                outcome.MarkAsWinner();
            }
            else
            {
                outcome.MarkAsLoser();
            }
        }

        IsSettled = true;
        IsOpen = false;
    }

    /// <summary>
    /// Void specific outcomes in this market
    /// </summary>
    public void VoidOutcomes(params Guid[] outcomeIds)
    {
        EnsureAttachedToEvent();

        foreach (var outcomeId in outcomeIds)
        {
            var outcome = _outcomes.FirstOrDefault(o => o.Id == outcomeId);
            if (outcome != null)
            {
                outcome.MarkAsVoid();
            }
        }
    }

    /// <summary>
    /// Settle the market as void (all outcomes voided)
    /// </summary>
    public void SettleAsVoid()
    {
        EnsureAttachedToEvent();

        if (IsSettled)
            throw new SettlementException("Market is already settled");

        foreach (var outcome in _outcomes)
        {
            outcome.MarkAsVoid();
        }

        IsSettled = true;
        IsOpen = false;
    }

    /// <summary>
    /// Settle the market as push (all outcomes pushed/tied)
    /// </summary>
    public void SettleAsPush()
    {
        EnsureAttachedToEvent();

        if (IsSettled)
            throw new SettlementException("Market is already settled");

        foreach (var outcome in _outcomes)
        {
            outcome.MarkAsPush();
        }

        IsSettled = true;
        IsOpen = false;
    }

    /// <summary>
    /// Get outcome by ID
    /// </summary>
    public Outcome? GetOutcome(Guid outcomeId)
    {
        return _outcomes.FirstOrDefault(o => o.Id == outcomeId);
    }

    public override string ToString() => $"{Name} ({Type})";
}
