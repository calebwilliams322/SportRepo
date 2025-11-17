using Microsoft.Extensions.Logging;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.ValueObjects;
using SportsBettingListener.OddsApi.Models;

namespace SportsBettingListener.Sync.Mappers;

/// <summary>
/// Maps OddsApiMarket data to SportsBetting Market and Outcome entities.
/// Handles different market types: moneyline (h2h), spreads, and totals.
/// </summary>
public class MarketMapper
{
    private readonly ILogger<MarketMapper> _logger;

    public MarketMapper(ILogger<MarketMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Maps all markets from an OddsApiEvent to Market entities.
    /// Uses the preferred bookmaker (DraftKings by default) for odds.
    /// </summary>
    public List<Market> MapMarketsForEvent(OddsApiEvent oddsEvent)
    {
        if (oddsEvent == null)
            throw new ArgumentNullException(nameof(oddsEvent));

        var markets = new List<Market>();

        // Get the preferred bookmaker (DraftKings first, or fallback to first available)
        var bookmaker = GetPreferredBookmaker(oddsEvent.Bookmakers);

        if (bookmaker == null)
        {
            _logger.LogWarning("No bookmakers available for event {EventId}", oddsEvent.Id);
            return markets;
        }

        _logger.LogDebug("Using bookmaker {Bookmaker} for event {EventId}", bookmaker.Title, oddsEvent.Id);

        // Map each market type
        foreach (var oddsMarket in bookmaker.Markets)
        {
            try
            {
                var market = oddsMarket.Key switch
                {
                    "h2h" => CreateMoneylineMarket(oddsMarket),
                    "spreads" => CreateSpreadMarket(oddsMarket),
                    "totals" => CreateTotalsMarket(oddsMarket),
                    _ => null
                };

                if (market != null)
                {
                    // Set external ID for tracking
                    market.SetExternalId(oddsMarket.Key);

                    // Enable Hybrid mode: allows both sportsbook (API odds) and exchange (P2P) betting
                    market.SetMarketMode(MarketMode.Hybrid, exchangeCommissionRate: 0.02m);

                    markets.Add(market);
                    _logger.LogDebug("Created {MarketType} market with {OutcomeCount} outcomes",
                        market.Type, market.Outcomes.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create market for type {MarketKey}", oddsMarket.Key);
            }
        }

        return markets;
    }

    /// <summary>
    /// Gets the preferred bookmaker from the list.
    /// Preference: DraftKings > FanDuel > first available
    /// </summary>
    private OddsApiBookmaker? GetPreferredBookmaker(List<OddsApiBookmaker> bookmakers)
    {
        if (bookmakers == null || bookmakers.Count == 0)
            return null;

        // Try to find DraftKings first
        var draftkings = bookmakers.FirstOrDefault(b => b.Key.Equals("draftkings", StringComparison.OrdinalIgnoreCase));
        if (draftkings != null)
            return draftkings;

        // Fallback to FanDuel
        var fanduel = bookmakers.FirstOrDefault(b => b.Key.Equals("fanduel", StringComparison.OrdinalIgnoreCase));
        if (fanduel != null)
            return fanduel;

        // Use first available
        return bookmakers.First();
    }

    /// <summary>
    /// Creates a Moneyline market from h2h odds.
    /// </summary>
    private Market CreateMoneylineMarket(OddsApiMarket oddsMarket)
    {
        var market = new Market(
            type: MarketType.Moneyline,
            name: "Moneyline",
            description: "Straight bet on which team wins"
        );

        // Create outcomes for each team
        foreach (var oddsOutcome in oddsMarket.Outcomes)
        {
            var odds = new Odds(oddsOutcome.Price);
            var outcome = new Outcome(
                name: oddsOutcome.Name,
                description: oddsOutcome.Name,
                initialOdds: odds,
                line: null // No line for moneyline
            );

            outcome.SetExternalId(oddsOutcome.Name);
            market.AddOutcome(outcome);
        }

        return market;
    }

    /// <summary>
    /// Creates a Point Spread market from spreads odds.
    /// </summary>
    private Market CreateSpreadMarket(OddsApiMarket oddsMarket)
    {
        var market = new Market(
            type: MarketType.Spread,
            name: "Point Spread",
            description: "Bet on team to cover the spread"
        );

        // Create outcomes for each team with their spread
        foreach (var oddsOutcome in oddsMarket.Outcomes)
        {
            if (!oddsOutcome.Point.HasValue)
            {
                _logger.LogWarning("Spread outcome missing point value for {Team}", oddsOutcome.Name);
                continue;
            }

            var odds = new Odds(oddsOutcome.Price);
            var spreadValue = oddsOutcome.Point.Value;

            // Description includes the spread (e.g., "Chiefs -7.5")
            var description = spreadValue > 0
                ? $"{oddsOutcome.Name} +{spreadValue:F1}"
                : $"{oddsOutcome.Name} {spreadValue:F1}";

            var outcome = new Outcome(
                name: oddsOutcome.Name,
                description: description,
                initialOdds: odds,
                line: spreadValue
            );

            outcome.SetExternalId($"{oddsOutcome.Name}_{spreadValue}");
            market.AddOutcome(outcome);
        }

        return market;
    }

    /// <summary>
    /// Creates a Totals (Over/Under) market from totals odds.
    /// </summary>
    private Market CreateTotalsMarket(OddsApiMarket oddsMarket)
    {
        // Extract the total line value (should be same for Over and Under)
        var totalLine = oddsMarket.Outcomes.FirstOrDefault()?.Point;
        if (!totalLine.HasValue)
        {
            throw new InvalidOperationException("Totals market missing point value");
        }

        var market = new Market(
            type: MarketType.Totals,
            name: "Total Points",
            description: $"Over/Under {totalLine:F1} total points"
        );

        // Create Over and Under outcomes
        foreach (var oddsOutcome in oddsMarket.Outcomes)
        {
            if (!oddsOutcome.Point.HasValue)
            {
                _logger.LogWarning("Totals outcome missing point value for {Outcome}", oddsOutcome.Name);
                continue;
            }

            var odds = new Odds(oddsOutcome.Price);
            var line = oddsOutcome.Point.Value;

            // Name is "Over" or "Under"
            var description = $"{oddsOutcome.Name} {line:F1}";

            var outcome = new Outcome(
                name: oddsOutcome.Name,
                description: description,
                initialOdds: odds,
                line: line
            );

            outcome.SetExternalId($"{oddsOutcome.Name}_{line}");
            market.AddOutcome(outcome);
        }

        return market;
    }

    /// <summary>
    /// Updates existing market outcomes with new odds from The Odds API.
    /// Used during periodic sync to update odds without recreating markets.
    /// </summary>
    public void UpdateMarketOdds(Market market, OddsApiMarket oddsMarket)
    {
        if (market == null)
            throw new ArgumentNullException(nameof(market));
        if (oddsMarket == null)
            throw new ArgumentNullException(nameof(oddsMarket));

        _logger.LogDebug("Updating odds for market {MarketName}", market.Name);

        foreach (var oddsOutcome in oddsMarket.Outcomes)
        {
            // Find matching outcome by name or external ID
            var outcome = market.Outcomes.FirstOrDefault(o =>
                o.Name.Equals(oddsOutcome.Name, StringComparison.OrdinalIgnoreCase) ||
                o.ExternalId == oddsOutcome.Name);

            if (outcome != null)
            {
                var newOdds = new Odds(oddsOutcome.Price);
                outcome.UpdateOddsFromApi(newOdds);
                _logger.LogDebug("Updated odds for outcome {Outcome}: {OldOdds} -> {NewOdds}",
                    outcome.Name, outcome.CurrentOdds, newOdds);
            }
            else
            {
                _logger.LogWarning("Could not find outcome {OutcomeName} in market {MarketName}",
                    oddsOutcome.Name, market.Name);
            }
        }
    }
}
