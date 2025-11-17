using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Exchange-specific details for a P2P bet
/// Links to a Bet and tracks matching status
/// </summary>
public class ExchangeBet
{
    public Guid Id { get; private set; }
    public Guid BetId { get; private set; }
    public Bet Bet { get; private set; } = null!;

    /// <summary>
    /// Side of the bet: Back (betting FOR) or Lay (betting AGAINST)
    /// </summary>
    public BetSide Side { get; private set; }

    /// <summary>
    /// Odds proposed by the user
    /// </summary>
    public decimal ProposedOdds { get; private set; }

    /// <summary>
    /// Total stake the user wants to place
    /// </summary>
    public decimal TotalStake { get; private set; }

    /// <summary>
    /// Amount that has been matched with opposing bets
    /// </summary>
    public decimal MatchedStake { get; private set; }

    /// <summary>
    /// Amount still waiting to be matched
    /// </summary>
    public decimal UnmatchedStake { get; private set; }

    /// <summary>
    /// Current state: Unmatched, PartiallyMatched, Matched, Cancelled
    /// </summary>
    public BetState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Navigation properties
    private readonly List<BetMatch> _matchesAsBack = new();
    private readonly List<BetMatch> _matchesAsLay = new();
    public IReadOnlyList<BetMatch> MatchesAsBack => _matchesAsBack.AsReadOnly();
    public IReadOnlyList<BetMatch> MatchesAsLay => _matchesAsLay.AsReadOnly();

    // EF Core constructor
    private ExchangeBet()
    {
    }

    public ExchangeBet(Bet bet, BetSide side, decimal proposedOdds, decimal totalStake)
    {
        if (bet.BetMode != BetMode.Exchange)
            throw new InvalidOperationException("Can only create ExchangeBet for exchange-mode bets");

        if (proposedOdds < 1.0m)
            throw new ArgumentException("Odds must be at least 1.0", nameof(proposedOdds));

        if (totalStake <= 0)
            throw new ArgumentException("Stake must be positive", nameof(totalStake));

        Id = Guid.NewGuid();
        BetId = bet.Id;
        Bet = bet;
        Side = side;
        ProposedOdds = proposedOdds;
        TotalStake = totalStake;
        MatchedStake = 0;
        UnmatchedStake = totalStake;
        State = BetState.Unmatched;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Apply a match to this bet, updating matched/unmatched stakes
    /// </summary>
    public void ApplyMatch(decimal matchedAmount)
    {
        if (matchedAmount <= 0)
            throw new ArgumentException("Matched amount must be positive", nameof(matchedAmount));

        if (matchedAmount > UnmatchedStake)
            throw new InvalidOperationException("Cannot match more than unmatched stake");

        MatchedStake += matchedAmount;
        UnmatchedStake -= matchedAmount;

        State = UnmatchedStake > 0 ? BetState.PartiallyMatched : BetState.Matched;
    }

    /// <summary>
    /// Cancel this bet (only if not fully matched)
    /// </summary>
    public void Cancel()
    {
        if (State == BetState.Matched)
            throw new InvalidOperationException("Cannot cancel fully matched bet");

        if (State == BetState.Cancelled)
            throw new InvalidOperationException("Bet already cancelled");

        State = BetState.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate liability for lay bets (what you can lose)
    /// </summary>
    public decimal CalculateLiability()
    {
        // For Back bets: liability is just the stake
        // For Lay bets: liability is what the backer wins if they win
        return Side == BetSide.Lay
            ? TotalStake * (ProposedOdds - 1)
            : TotalStake;
    }
}
