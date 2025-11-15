using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a betting ticket/slip placed by a user
/// Can contain single or multiple selections (parlay)
/// </summary>
public class Bet
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }
    public string TicketNumber { get; }
    public BetType Type { get; }

    private readonly List<BetSelection> _selections;
    public IReadOnlyList<BetSelection> Selections => _selections.AsReadOnly();

    public Money Stake { get; }
    public BetStatus Status { get; private set; }

    /// <summary>
    /// Navigation property to the user who placed this bet
    /// </summary>
    public User? User { get; private set; }

    public DateTime PlacedAt { get; }
    public DateTime? SettledAt { get; private set; }

    /// <summary>
    /// The total payout if bet wins (including stake)
    /// </summary>
    public Money PotentialPayout { get; }

    /// <summary>
    /// Actual payout after settlement (null until settled)
    /// </summary>
    public Money? ActualPayout { get; private set; }

    /// <summary>
    /// Combined odds for the bet
    /// For single bets: the selection's odds
    /// For parlays: product of all selection odds
    /// </summary>
    public Odds CombinedOdds { get; }

    /// <summary>
    /// If this bet was created from a LineLock, this contains the LineLock ID
    /// </summary>
    public Guid? LineLockId { get; }

    /// <summary>
    /// Whether this bet was created by exercising a LineLock
    /// </summary>
    public bool WasLineLocked => LineLockId.HasValue;

    // Private parameterless constructor for EF Core
    private Bet()
    {
        _selections = new List<BetSelection>();
        TicketNumber = string.Empty;
    }

    private Bet(
        User user,
        BetType type,
        Money stake,
        List<BetSelection> selections,
        Guid? lineLockId = null)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (selections == null || selections.Count == 0)
            throw new ArgumentException("Bet must have at least one selection", nameof(selections));

        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;
        TicketNumber = GenerateTicketNumber();
        Type = type;
        Stake = stake;
        _selections = selections;
        PlacedAt = DateTime.UtcNow;
        Status = BetStatus.Pending;
        LineLockId = lineLockId;

        // Set bet ID on all selections
        foreach (var selection in _selections)
        {
            selection.SetBetId(Id);
        }

        // Calculate combined odds
        CombinedOdds = CalculateCombinedOdds();
        PotentialPayout = CombinedOdds.CalculatePayout(Stake);
    }

    /// <summary>
    /// Create a single bet
    /// </summary>
    public static Bet CreateSingle(
        User user,
        Money stake,
        Event evt,
        Market market,
        Outcome outcome)
    {
        if (!market.IsOpen)
            throw new MarketClosedException($"Market {market.Name} is closed");

        var selection = new BetSelection(evt, market, outcome);
        return new Bet(user, BetType.Single, stake, new List<BetSelection> { selection });
    }

    /// <summary>
    /// Create a parlay bet with multiple selections
    /// </summary>
    public static Bet CreateParlay(
        User user,
        Money stake,
        params (Event evt, Market market, Outcome outcome)[] selections)
    {
        if (selections == null || selections.Length < 2)
            throw new InvalidBetException("Parlay must have at least 2 selections");

        // Validate all markets are open
        foreach (var (_, market, _) in selections)
        {
            if (!market.IsOpen)
                throw new MarketClosedException($"Market {market.Name} is closed");
        }

        // Check for same event (basic validation - some sportsbooks allow same-game parlays with special handling)
        var eventIds = selections.Select(s => s.evt.Id).Distinct().ToList();
        if (eventIds.Count < selections.Length)
        {
            // Same event parlay - would need special correlation handling
            // For now, we'll allow it but note it's a same-game parlay
        }

        var betSelections = selections
            .Select(s => new BetSelection(s.evt, s.market, s.outcome))
            .ToList();

        return new Bet(user, BetType.Parlay, stake, betSelections);
    }

    /// <summary>
    /// Create a single bet from a LineLock with locked odds
    /// This is an internal method called by LineLock.Exercise()
    /// </summary>
    internal static Bet CreateSingleFromLineLock(
        User user,
        Money stake,
        Event evt,
        Market market,
        Outcome outcome,
        Odds lockedOdds,
        LineLock lineLock)
    {
        if (lineLock == null)
            throw new ArgumentNullException(nameof(lineLock));

        // Note: We don't check if market is open because LineLock might be exercised
        // when market odds have moved but the lock guarantees the price

        var selection = new BetSelection(evt, market, outcome, lockedOdds);
        return new Bet(user, BetType.Single, stake, new List<BetSelection> { selection }, lineLock.Id);
    }

    /// <summary>
    /// Settle the bet based on selection outcomes
    /// </summary>
    public void Settle()
    {
        if (Status != BetStatus.Pending)
            throw new SettlementException($"Cannot settle bet in {Status} status");

        // First, settle all individual selections
        foreach (var selection in _selections)
        {
            if (selection.IsPending)
            {
                throw new SettlementException($"Selection {selection.Id} is not yet settled");
            }
        }

        // Determine bet outcome based on type
        switch (Type)
        {
            case BetType.Single:
                SettleSingleBet();
                break;

            case BetType.Parlay:
                SettleParlayBet();
                break;

            default:
                throw new NotImplementedException($"Settlement for {Type} bets not implemented");
        }

        SettledAt = DateTime.UtcNow;
    }

    private void SettleSingleBet()
    {
        var selection = _selections[0];

        if (selection.IsVoid)
        {
            // Void - return stake
            Status = BetStatus.Void;
            ActualPayout = Stake;
        }
        else if (selection.IsPush)
        {
            // Push - return stake
            Status = BetStatus.Pushed;
            ActualPayout = Stake;
        }
        else if (selection.IsWin)
        {
            // Win - pay out based on odds
            Status = BetStatus.Won;
            ActualPayout = PotentialPayout;
        }
        else
        {
            // Loss
            Status = BetStatus.Lost;
            ActualPayout = Money.Zero(Stake.Currency);
        }
    }

    private void SettleParlayBet()
    {
        // Count wins, losses, voids, pushes
        var wins = _selections.Count(s => s.IsWin);
        var losses = _selections.Count(s => s.IsLoss);
        var voids = _selections.Count(s => s.IsVoid);
        var pushes = _selections.Count(s => s.IsPush);

        // If any leg lost, entire parlay loses
        if (losses > 0)
        {
            Status = BetStatus.Lost;
            ActualPayout = Money.Zero(Stake.Currency);
            return;
        }

        // If all legs are void, return stake
        if (voids == _selections.Count)
        {
            Status = BetStatus.Void;
            ActualPayout = Stake;
            return;
        }

        // If some legs voided/pushed but none lost, recalculate odds without voided legs
        if (voids > 0 || pushes > 0)
        {
            var activeSelections = _selections.Where(s => s.IsWin).ToList();

            if (activeSelections.Count == 0)
            {
                // All legs pushed/voided
                Status = BetStatus.Pushed;
                ActualPayout = Stake;
                return;
            }

            if (activeSelections.Count == 1)
            {
                // Only one active leg - treat as single
                var recalculatedOdds = activeSelections[0].LockedOdds;
                Status = BetStatus.Won;
                ActualPayout = recalculatedOdds.CalculatePayout(Stake);
                return;
            }

            // Multiple active legs - recalculate parlay odds
            var recalculatedParlayOdds = activeSelections
                .Select(s => s.LockedOdds)
                .Aggregate((a, b) => a * b);

            Status = BetStatus.Won;
            ActualPayout = recalculatedParlayOdds.CalculatePayout(Stake);
            return;
        }

        // All legs won
        if (wins == _selections.Count)
        {
            Status = BetStatus.Won;
            ActualPayout = PotentialPayout;
            return;
        }

        throw new SettlementException("Unexpected parlay state during settlement");
    }

    /// <summary>
    /// Manually void this bet (e.g., if event is cancelled)
    /// </summary>
    public void Void()
    {
        if (Status == BetStatus.Won || Status == BetStatus.Lost)
            throw new InvalidOperationException("Cannot void a settled bet");

        Status = BetStatus.Void;
        ActualPayout = Stake; // Return stake
        SettledAt = DateTime.UtcNow;

        // Mark all selections as void
        foreach (var selection in _selections)
        {
            selection.MarkAsVoid();
        }
    }

    private Odds CalculateCombinedOdds()
    {
        if (_selections.Count == 1)
        {
            return _selections[0].LockedOdds;
        }

        // Parlay odds = product of all selection odds
        return _selections
            .Select(s => s.LockedOdds)
            .Aggregate((a, b) => a * b);
    }

    private static string GenerateTicketNumber()
    {
        // Generate a readable ticket number (could be more sophisticated)
        return $"BET{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    public Money PotentialProfit => PotentialPayout - Stake;

    public Money? ActualProfit => ActualPayout != null ? ActualPayout - Stake : null;

    public bool IsPending => Status == BetStatus.Pending || Status == BetStatus.PendingAcceptance;

    public bool IsSettled => Status == BetStatus.Won
                             || Status == BetStatus.Lost
                             || Status == BetStatus.Pushed
                             || Status == BetStatus.Void;

    public override string ToString()
    {
        var selectionsStr = string.Join(", ", _selections.Select(s => s.OutcomeName));
        return $"Ticket #{TicketNumber}: {Type} - {selectionsStr} - Stake: {Stake} - Potential: {PotentialPayout}";
    }
}
