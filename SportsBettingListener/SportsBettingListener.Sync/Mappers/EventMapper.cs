using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBettingListener.OddsApi.Models;

namespace SportsBettingListener.Sync.Mappers;

/// <summary>
/// Maps OddsApiEvent data to SportsBetting Event domain entities.
/// Handles lookup/creation of related entities (Sport, League, Team).
/// </summary>
public class EventMapper
{
    private readonly SportsBettingDbContext _context;
    private readonly ILogger<EventMapper> _logger;

    public EventMapper(SportsBettingDbContext context, ILogger<EventMapper> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Maps an OddsApiEvent to a SportsBetting Event entity.
    /// Creates all necessary related entities (Sport, League, Teams) if they don't exist.
    /// </summary>
    public async Task<Event> MapToEventAsync(OddsApiEvent oddsEvent, CancellationToken cancellationToken = default)
    {
        if (oddsEvent == null)
            throw new ArgumentNullException(nameof(oddsEvent));

        _logger.LogDebug("Mapping event: {HomeTeam} vs {AwayTeam}", oddsEvent.HomeTeam, oddsEvent.AwayTeam);

        // 1. Get or create Sport
        var sport = await GetOrCreateSportAsync(oddsEvent.SportKey, oddsEvent.SportTitle, cancellationToken);

        // 2. Get or create League (use sport title as league for now)
        var league = await GetOrCreateLeagueAsync(oddsEvent.SportKey, oddsEvent.SportTitle, sport.Id, cancellationToken);

        // 3. Get or create Teams
        var homeTeam = await GetOrCreateTeamAsync(oddsEvent.HomeTeam, league.Id, cancellationToken);
        var awayTeam = await GetOrCreateTeamAsync(oddsEvent.AwayTeam, league.Id, cancellationToken);

        // 4. Create Event entity
        var eventName = $"{oddsEvent.HomeTeam} vs {oddsEvent.AwayTeam}";
        var evt = new Event(
            name: eventName,
            homeTeam: homeTeam,
            awayTeam: awayTeam,
            scheduledStartTime: oddsEvent.CommenceTime.ToUniversalTime(),
            leagueId: league.Id,
            venue: null // The Odds API doesn't provide venue information
        );

        // 5. Set external ID for API syncing
        evt.SetExternalId(oddsEvent.Id);

        _logger.LogInformation("Mapped event: {EventName} (ExternalId: {ExternalId})", eventName, oddsEvent.Id);

        return evt;
    }

    /// <summary>
    /// Gets an existing Sport by external key or creates a new one.
    /// Does not save changes - caller must call SaveChangesAsync.
    /// </summary>
    private async Task<Sport> GetOrCreateSportAsync(
        string sportKey,
        string sportTitle,
        CancellationToken cancellationToken)
    {
        // Look up by sport key (e.g., "americanfootball_nfl")
        // We'll store the sport key as the Code
        var sportCode = sportKey.ToUpperInvariant();
        var sport = await _context.Sports
            .FirstOrDefaultAsync(s => s.Code == sportCode, cancellationToken);

        if (sport == null)
        {
            _logger.LogInformation("Creating new sport: {SportTitle} ({SportKey})", sportTitle, sportKey);

            sport = new Sport(
                name: sportTitle,
                code: sportCode
            );

            _context.Sports.Add(sport);
            _logger.LogDebug("Sport {SportCode} added to context (will be saved with transaction)", sportCode);
        }

        return sport;
    }

    /// <summary>
    /// Gets an existing League by code or creates a new one.
    /// Does not save changes - caller must call SaveChangesAsync.
    /// </summary>
    private async Task<League> GetOrCreateLeagueAsync(
        string sportKey,
        string leagueName,
        Guid sportId,
        CancellationToken cancellationToken)
    {
        // Use sport key as league code (e.g., "AMERICANFOOTBALL_NFL" -> "NFL")
        // Extract the last part after underscore if present
        var leagueCode = ExtractLeagueCode(sportKey);

        var league = await _context.Leagues
            .FirstOrDefaultAsync(l => l.Code == leagueCode && l.SportId == sportId, cancellationToken);

        if (league == null)
        {
            _logger.LogInformation("Creating new league: {LeagueName} ({LeagueCode})", leagueName, leagueCode);

            league = new League(
                name: leagueName,
                code: leagueCode,
                sportId: sportId
            );

            _context.Leagues.Add(league);
            _logger.LogDebug("League {LeagueCode} added to context (will be saved with transaction)", leagueCode);
        }

        return league;
    }

    /// <summary>
    /// Gets an existing Team by name or creates a new one.
    /// Does not save changes - caller must call SaveChangesAsync.
    /// </summary>
    private async Task<Team> GetOrCreateTeamAsync(
        string teamName,
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        // Parse city and team name FIRST so we can look up consistently
        var (city, name) = ParseTeamName(teamName);

        // Look up team by PARSED name within the league (not full name)
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Name == name && t.LeagueId == leagueId, cancellationToken);

        if (team == null)
        {
            _logger.LogInformation("Creating new team: {TeamName} (parsed as: {ParsedName})", teamName, name);

            // Generate a unique team code (handles collisions)
            var teamCode = await GenerateUniqueTeamCodeAsync(name, leagueId, cancellationToken);

            team = new Team(
                name: name,
                code: teamCode,
                leagueId: leagueId,
                city: city
            );

            _context.Teams.Add(team);
            _logger.LogDebug("Team {TeamName} added to context with code {TeamCode}", name, teamCode);
        }

        return team;
    }

    /// <summary>
    /// Extracts league code from sport key.
    /// Example: "americanfootball_nfl" -> "NFL"
    /// </summary>
    private static string ExtractLeagueCode(string sportKey)
    {
        var parts = sportKey.Split('_');
        return parts.Length > 1
            ? parts[^1].ToUpperInvariant()
            : sportKey.ToUpperInvariant();
    }

    /// <summary>
    /// Parses team name into city and name components.
    /// Example: "Kansas City Chiefs" -> ("Kansas City", "Chiefs")
    /// Example: "Liverpool" -> (null, "Liverpool")
    /// </summary>
    private static (string? city, string name) ParseTeamName(string fullName)
    {
        // Common city patterns (this is simplistic, could be enhanced)
        var commonCities = new[]
        {
            "New York", "Los Angeles", "San Francisco", "Kansas City",
            "New Orleans", "Green Bay", "Tampa Bay", "Las Vegas"
        };

        foreach (var city in commonCities)
        {
            if (fullName.StartsWith(city, StringComparison.OrdinalIgnoreCase))
            {
                var name = fullName.Substring(city.Length).Trim();
                return (city, name);
            }
        }

        // Try simple two-word city pattern
        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 3)
        {
            // Assume first two words are city, rest is team name
            var city = $"{words[0]} {words[1]}";
            var name = string.Join(" ", words.Skip(2));
            return (city, name);
        }

        // Single word team or couldn't parse city
        return (null, fullName);
    }

    /// <summary>
    /// Generates a team code from team name.
    /// Example: "Chiefs" -> "CHI" (simplified - just takes first letters)
    /// </summary>
    private static string GenerateTeamCode(string teamName)
    {
        var words = teamName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            // Single word: take first 3 letters
            return teamName.Substring(0, Math.Min(3, teamName.Length)).ToUpperInvariant();
        }
        else
        {
            // Multiple words: take first letter of each
            var code = string.Join("", words.Select(w => w[0]));
            return code.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Generates a unique team code by checking for collisions and appending numbers if needed.
    /// Example: "CB" exists -> try "CB2" -> "CB3" etc.
    /// </summary>
    private async Task<string> GenerateUniqueTeamCodeAsync(
        string teamName,
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        var baseCode = GenerateTeamCode(teamName);
        var code = baseCode;
        var attempt = 1;

        // Keep trying until we find a unique code
        while (await IsTeamCodeTakenAsync(code, leagueId, cancellationToken))
        {
            attempt++;
            code = $"{baseCode}{attempt}";

            if (attempt > 100)
            {
                // Safety check - should never happen
                throw new InvalidOperationException($"Could not generate unique team code for {teamName} after 100 attempts");
            }
        }

        if (attempt > 1)
        {
            _logger.LogInformation("Team code collision detected for {TeamName}. Using {Code} instead of {BaseCode}",
                teamName, code, baseCode);
        }

        return code;
    }

    /// <summary>
    /// Checks if a team code is already taken globally (both in database and current context).
    /// Note: Team codes must be unique across all leagues due to database constraint.
    /// </summary>
    private async Task<bool> IsTeamCodeTakenAsync(
        string code,
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        // Check database (global check - team codes are unique across all sports)
        var existsInDb = await _context.Teams
            .AnyAsync(t => t.Code == code, cancellationToken);

        if (existsInDb)
            return true;

        // Check current context (teams being created in this transaction)
        var existsInContext = _context.ChangeTracker.Entries<Team>()
            .Any(e => e.State == EntityState.Added &&
                      e.Entity.Code == code);

        return existsInContext;
    }
}
