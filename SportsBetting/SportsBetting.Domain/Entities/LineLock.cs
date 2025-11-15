using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a LineLock - an option contract that locks in specific odds for future bet placement.
/// User pays a premium (lock fee) to guarantee odds for a limited time and max stake amount.
/// Similar to a financial option - right but not obligation to place bet at locked price.
/// </summary>
public class LineLock
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }
    public string LockNumber { get; }

    public Guid EventId { get; }
    public string EventName { get; }

    public Guid MarketId { get; }
    public MarketType MarketType { get; }
    public string MarketName { get; }

    public Guid OutcomeId { get; }
    public string OutcomeName { get; }

    /// <summary>
    /// The odds locked in for this lock
    /// </summary>
    public Odds LockedOdds { get; }

    /// <summary>
    /// The line value (for spread/totals), if applicable
    /// </summary>
    public decimal? Line { get; }

    /// <summary>
    /// The fee (premium) paid to purchase this lock
    /// </summary>
    public Money LockFee { get; }

    /// <summary>
    /// Maximum stake amount this lock covers
    /// User can exercise with less, but not more
    /// </summary>
    public Money MaxStake { get; }

    /// <summary>
    /// When this lock expires (cannot exercise after this time)
    /// </summary>
    public DateTime ExpirationTime { get; }

    /// <summary>
    /// When the lock was created
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Current status of the lock
    /// </summary>
    public LineLockStatus Status { get; private set; }

    /// <summary>
    /// If exercised, reference to the bet that was created
    /// </summary>
    public Guid? AssociatedBetId { get; private set; }

    /// <summary>
    /// When the lock was exercised/expired/cancelled
    /// </summary>
    public DateTime? SettledAt { get; private set; }

    /// <summary>
    /// Navigation property to the user who owns this lock
    /// </summary>
    public User? User { get; private set; }

    // Private parameterless constructor for EF Core
    private LineLock()
    {
        // Initialize required non-nullable string properties
        LockNumber = string.Empty;
        EventName = string.Empty;
        MarketName = string.Empty;
        OutcomeName = string.Empty;
        // Initialize value objects
        LockedOdds = new Odds(1.0m);
        LockFee = Money.Zero();
        MaxStake = Money.Zero();
    }

    private LineLock(
        User user,
        Event evt,
        Market market,
        Outcome outcome,
        Money lockFee,
        Money maxStake,
        DateTime expirationTime)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));
        if (market == null)
            throw new ArgumentNullException(nameof(market));
        if (outcome == null)
            throw new ArgumentNullException(nameof(outcome));
        if (expirationTime <= DateTime.UtcNow)
            throw new InvalidBetException("LineLock expiration time must be in the future");
        if (expirationTime >= evt.ScheduledStartTime)
            throw new InvalidBetException("LineLock cannot expire after event starts");

        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;
        LockNumber = GenerateLockNumber();

        EventId = evt.Id;
        EventName = evt.Name;
        MarketId = market.Id;
        MarketType = market.Type;
        MarketName = market.Name;
        OutcomeId = outcome.Id;
        OutcomeName = outcome.Name;

        LockedOdds = outcome.CurrentOdds; // Lock in current odds
        Line = outcome.Line;

        LockFee = lockFee;
        MaxStake = maxStake;
        ExpirationTime = expirationTime;
        CreatedAt = DateTime.UtcNow;
        Status = LineLockStatus.Active;
    }

    /// <summary>
    /// Creates a new LineLock on a specific outcome
    /// </summary>
    public static LineLock Create(
        User user,
        Event evt,
        Market market,
        Outcome outcome,
        Money lockFee,
        Money maxStake,
        DateTime expirationTime)
    {
        if (!market.IsOpen)
            throw new MarketClosedException($"Cannot create LineLock on closed market {market.Name}");

        return new LineLock(user, evt, market, outcome, lockFee, maxStake, expirationTime);
    }

    /// <summary>
    /// Exercise the lock by converting it to an actual bet
    /// </summary>
    /// <param name="stake">Amount to wager (must be ≤ MaxStake)</param>
    /// <param name="evt">Event reference for bet creation</param>
    /// <param name="market">Market reference for bet creation</param>
    /// <param name="outcome">Outcome reference for bet creation</param>
    /// <returns>The created bet</returns>
    public Bet Exercise(
        Money stake,
        Event evt,
        Market market,
        Outcome outcome)
    {
        // Validation
        if (Status != LineLockStatus.Active)
            throw new InvalidBetException($"Cannot exercise LineLock in {Status} status");

        if (DateTime.UtcNow > ExpirationTime)
        {
            // Auto-expire if time has passed
            Expire();
            throw new InvalidBetException("LineLock has expired");
        }

        if (stake > MaxStake)
            throw new InvalidBetException($"Stake {stake} exceeds max stake {MaxStake} for this LineLock");

        if (stake.Currency != MaxStake.Currency)
            throw new InvalidBetException("Stake currency must match LineLock currency");

        // Verify references match
        if (evt.Id != EventId || market.Id != MarketId || outcome.Id != OutcomeId)
            throw new InvalidBetException("Event/Market/Outcome must match the LineLock");

        if (User == null)
            throw new InvalidOperationException("LineLock must have an associated user");

        // Create the bet with locked odds (even if market odds have changed)
        var bet = Bet.CreateSingleFromLineLock(User, stake, evt, market, outcome, LockedOdds, this);

        // Mark lock as used
        Status = LineLockStatus.Used;
        AssociatedBetId = bet.Id;
        SettledAt = DateTime.UtcNow;

        return bet;
    }

    /// <summary>
    /// Mark the lock as expired (time ran out)
    /// </summary>
    public void Expire()
    {
        if (Status != LineLockStatus.Active)
            throw new InvalidOperationException($"Cannot expire LineLock in {Status} status");

        Status = LineLockStatus.Expired;
        SettledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel the lock (e.g., event cancelled)
    /// In this case, the lock fee should be refunded
    /// </summary>
    public void Cancel()
    {
        if (Status == LineLockStatus.Used)
            throw new InvalidOperationException("Cannot cancel a LineLock that has been exercised");

        Status = LineLockStatus.Cancelled;
        SettledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the lock can still be exercised
    /// </summary>
    public bool CanExercise => Status == LineLockStatus.Active && DateTime.UtcNow <= ExpirationTime;

    /// <summary>
    /// Potential payout if exercised at max stake
    /// </summary>
    public Money MaxPotentialPayout => LockedOdds.CalculatePayout(MaxStake);

    /// <summary>
    /// Calculate potential payout for a given stake
    /// </summary>
    public Money CalculatePotentialPayout(Money stake)
    {
        if (stake > MaxStake)
            throw new ArgumentException($"Stake cannot exceed max stake of {MaxStake}");

        return LockedOdds.CalculatePayout(stake);
    }

    /// <summary>
    /// Time remaining until expiration
    /// </summary>
    public TimeSpan TimeRemaining
    {
        get
        {
            var remaining = ExpirationTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private static string GenerateLockNumber()
    {
        return $"LOCK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    public override string ToString()
    {
        var line = Line.HasValue ? $" {Line:+0.#;-0.#}" : "";
        var status = Status == LineLockStatus.Active ? $"Expires in {TimeRemaining.TotalMinutes:F1}m" : Status.ToString();
        return $"Lock #{LockNumber}: {OutcomeName}{line} @ {LockedOdds} - Max: {MaxStake} - {status}";
    }
}
