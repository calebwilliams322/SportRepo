using SportsBetting.Domain.Enums;

namespace SportsBetting.API.DTOs;

/// <summary>
/// Request to place an exchange bet
/// </summary>
public class PlaceExchangeBetRequest
{
    /// <summary>
    /// The outcome to bet on
    /// </summary>
    public Guid OutcomeId { get; set; }

    /// <summary>
    /// Back (betting FOR) or Lay (betting AGAINST)
    /// </summary>
    public BetSide Side { get; set; }

    /// <summary>
    /// The odds being proposed (e.g., 2.50)
    /// Must be >= 1.0
    /// </summary>
    public decimal ProposedOdds { get; set; }

    /// <summary>
    /// The stake amount
    /// </summary>
    public decimal Stake { get; set; }
}

/// <summary>
/// Response after placing an exchange bet
/// </summary>
public class PlaceExchangeBetResponse
{
    /// <summary>
    /// The created exchange bet ID
    /// </summary>
    public Guid ExchangeBetId { get; set; }

    /// <summary>
    /// Whether the bet was fully matched immediately
    /// </summary>
    public bool FullyMatched { get; set; }

    /// <summary>
    /// Amount that was matched
    /// </summary>
    public decimal MatchedAmount { get; set; }

    /// <summary>
    /// Amount still unmatched
    /// </summary>
    public decimal UnmatchedAmount { get; set; }

    /// <summary>
    /// Number of matches created
    /// </summary>
    public int MatchCount { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Odds validation result (warnings if outside 20% tolerance)
    /// </summary>
    public OddsValidationDto? OddsValidation { get; set; }
}

/// <summary>
/// Response for getting unmatched bets
/// </summary>
public class ExchangeBetDto
{
    public Guid Id { get; set; }
    public Guid BetId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public BetSide Side { get; set; }
    public string SideName { get; set; } = string.Empty;
    public decimal ProposedOdds { get; set; }
    public decimal TotalStake { get; set; }
    public decimal MatchedStake { get; set; }
    public decimal UnmatchedStake { get; set; }
    public BetState State { get; set; }
    public string StateName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to take (match) a specific exchange bet
/// </summary>
public class TakeBetRequest
{
    /// <summary>
    /// Amount of the bet to match (must be <= unmatched stake)
    /// </summary>
    public decimal StakeToMatch { get; set; }
}

/// <summary>
/// Response after taking a bet
/// </summary>
public class TakeBetResponse
{
    /// <summary>
    /// The match that was created
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// Your counter-bet ID
    /// </summary>
    public Guid YourBetId { get; set; }

    /// <summary>
    /// Amount matched
    /// </summary>
    public decimal MatchedAmount { get; set; }

    /// <summary>
    /// Odds matched at
    /// </summary>
    public decimal MatchedOdds { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Odds validation result
/// </summary>
public class OddsValidationDto
{
    public bool IsValid { get; set; }
    public decimal ConsensusOdds { get; set; }
    public decimal ProposedOdds { get; set; }
    public decimal DeviationPercent { get; set; }
    public bool HasWarning { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Consensus odds information
/// </summary>
public class ConsensusOddsDto
{
    public Guid OutcomeId { get; set; }
    public string OutcomeName { get; set; } = string.Empty;
    public decimal AverageOdds { get; set; }
    public decimal MinOdds { get; set; }
    public decimal MaxOdds { get; set; }
    public int SampleSize { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}
