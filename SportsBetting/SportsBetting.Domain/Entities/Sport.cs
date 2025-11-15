namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a sport/discipline (e.g., Football, Basketball, Tennis)
/// </summary>
public class Sport
{
    public Guid Id { get; }
    public string Name { get; }
    public string Code { get; } // e.g., "NFL", "NBA", "EPL"

    private readonly List<League> _leagues;
    public IReadOnlyList<League> Leagues => _leagues.AsReadOnly();

    // Private parameterless constructor for EF Core
    private Sport()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        Code = string.Empty;
        // Initialize collections
        _leagues = new List<League>();
    }

    public Sport(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sport name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Sport code cannot be empty", nameof(code));

        Id = Guid.NewGuid();
        Name = name;
        Code = code.ToUpperInvariant();
        _leagues = new List<League>();
    }

    public void AddLeague(League league)
    {
        if (league == null)
            throw new ArgumentNullException(nameof(league));

        if (_leagues.Any(l => l.Code == league.Code))
            throw new InvalidOperationException($"League with code {league.Code} already exists in this sport");

        _leagues.Add(league);
    }

    public override string ToString() => $"{Name} ({Code})";
}
