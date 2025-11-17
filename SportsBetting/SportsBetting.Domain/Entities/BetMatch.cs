using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a matched pair of exchange bets (one Back, one Lay)
/// Records the terms of the match and tracks settlement
/// </summary>
public class BetMatch
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The back bet (betting FOR the outcome)
    /// </summary>
    public Guid BackBetId { get; private set; }
    public ExchangeBet BackBet { get; private set; } = null!;

    /// <summary>
    /// The lay bet (betting AGAINST the outcome)
    /// </summary>
    public Guid LayBetId { get; private set; }
    public ExchangeBet LayBet { get; private set; } = null!;

    /// <summary>
    /// The amount of stake matched between these two bets
    /// </summary>
    public decimal MatchedStake { get; private set; }

    /// <summary>
    /// The odds at which this match was made
    /// </summary>
    public decimal MatchedOdds { get; private set; }

    /// <summary>
    /// When the match was created
    /// </summary>
    public DateTime MatchedAt { get; private set; }

    /// <summary>
    /// Whether this match has been settled (winner determined, payouts made)
    /// </summary>
    public bool IsSettled { get; private set; }

    /// <summary>
    /// The bet ID of the winner (BackBetId if outcome won, LayBetId if outcome lost)
    /// </summary>
    public Guid? WinnerBetId { get; private set; }

    /// <summary>
    /// When this match was settled
    /// </summary>
    public DateTime? SettledAt { get; private set; }

    /// <summary>
    /// Which bet was the maker (placed first, provided liquidity)
    /// </summary>
    public Guid MakerBetId { get; private set; }

    /// <summary>
    /// Which bet was the taker (matched existing order, took liquidity)
    /// </summary>
    public Guid TakerBetId { get; private set; }

    /// <summary>
    /// Commission charged to back bet winner (if any)
    /// </summary>
    public decimal? BackBetCommission { get; private set; }

    /// <summary>
    /// Commission charged to lay bet winner (if any)
    /// </summary>
    public decimal? LayBetCommission { get; private set; }

    // EF Core constructor
    private BetMatch()
    {
    }

    public BetMatch(
        ExchangeBet backBet,
        ExchangeBet layBet,
        decimal matchedStake,
        decimal matchedOdds,
        ExchangeBet makerBet)
    {
        if (backBet.Side != BetSide.Back)
            throw new ArgumentException("First bet must be a back bet", nameof(backBet));

        if (layBet.Side != BetSide.Lay)
            throw new ArgumentException("Second bet must be a lay bet", nameof(layBet));

        if (matchedStake <= 0)
            throw new ArgumentException("Matched stake must be positive", nameof(matchedStake));

        if (matchedOdds < 1.0m)
            throw new ArgumentException("Matched odds must be at least 1.0", nameof(matchedOdds));

        if (makerBet.Id != backBet.Id && makerBet.Id != layBet.Id)
            throw new ArgumentException("Maker bet must be either the back or lay bet", nameof(makerBet));

        Id = Guid.NewGuid();
        BackBetId = backBet.Id;
        BackBet = backBet;
        LayBetId = layBet.Id;
        LayBet = layBet;
        MatchedStake = matchedStake;
        MatchedOdds = matchedOdds;
        MatchedAt = DateTime.UtcNow;
        IsSettled = false;

        // Set maker/taker
        MakerBetId = makerBet.Id;
        TakerBetId = makerBet.Id == backBet.Id ? layBet.Id : backBet.Id;
    }

    /// <summary>
    /// Settle this match by determining the winner and recording commission
    /// </summary>
    /// <param name="backBetWins">True if the outcome occurred (back bet wins), false if not (lay bet wins)</param>
    /// <param name="backCommission">Commission charged to back bet (if winner)</param>
    /// <param name="layCommission">Commission charged to lay bet (if winner)</param>
    public void Settle(bool backBetWins, decimal backCommission = 0, decimal layCommission = 0)
    {
        if (IsSettled)
            throw new InvalidOperationException("Match already settled");

        WinnerBetId = backBetWins ? BackBetId : LayBetId;
        BackBetCommission = backBetWins ? backCommission : 0;
        LayBetCommission = !backBetWins ? layCommission : 0;
        IsSettled = true;
        SettledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the liquidity role for a specific bet in this match
    /// </summary>
    public LiquidityRole GetLiquidityRole(Guid betId)
    {
        if (betId == MakerBetId)
            return LiquidityRole.Maker;
        if (betId == TakerBetId)
            return LiquidityRole.Taker;

        throw new ArgumentException("Bet ID not found in this match", nameof(betId));
    }

    /// <summary>
    /// Calculate gross winnings (before commission) for the winner
    /// </summary>
    public decimal CalculateGrossWinnings()
    {
        // Winner gets their stake back plus profit
        // Profit = stake * (odds - 1)
        return MatchedStake * (MatchedOdds - 1);
    }

    /// <summary>
    /// Calculate net winnings after commission
    /// </summary>
    /// <param name="commissionRate">Commission rate (e.g., 0.02 for 2%)</param>
    public decimal CalculateNetWinnings(decimal commissionRate)
    {
        var grossWinnings = CalculateGrossWinnings();
        var commission = grossWinnings * commissionRate;
        return grossWinnings - commission;
    }
}
