namespace SportsBetting.Domain.Exceptions;

/// <summary>
/// Base exception for all betting-related errors
/// </summary>
public class BettingException : Exception
{
    public BettingException(string message) : base(message)
    {
    }

    public BettingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when attempting to place a bet on a closed or invalid market
/// </summary>
public class MarketClosedException : BettingException
{
    public MarketClosedException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when attempting invalid operations on an event
/// </summary>
public class InvalidEventStateException : BettingException
{
    public InvalidEventStateException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when bet validation fails
/// </summary>
public class InvalidBetException : BettingException
{
    public InvalidBetException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when settlement fails
/// </summary>
public class SettlementException : BettingException
{
    public SettlementException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when a user has insufficient funds for an operation
/// </summary>
public class InsufficientFundsException : BettingException
{
    public InsufficientFundsException(string message) : base(message)
    {
    }
}
