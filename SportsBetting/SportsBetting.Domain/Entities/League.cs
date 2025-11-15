namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a league or tournament (e.g., NFL, Premier League, NBA)
/// </summary>
public class League
{
    public Guid Id { get; }
    public string Name { get; }
    public string Code { get; } // e.g., "NFL", "EPL", "NBA"
    public Guid SportId { get; }

    private readonly List<Team> _teams;
    public IReadOnlyList<Team> Teams => _teams.AsReadOnly();

    // Private parameterless constructor for EF Core
    private League()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        Code = string.Empty;
        // Initialize collections
        _teams = new List<Team>();
    }

    public League(string name, string code, Guid sportId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("League name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("League code cannot be empty", nameof(code));

        Id = Guid.NewGuid();
        Name = name;
        Code = code.ToUpperInvariant();
        SportId = sportId;
        _teams = new List<Team>();
    }

    public void AddTeam(Team team)
    {
        if (team == null)
            throw new ArgumentNullException(nameof(team));

        if (_teams.Any(t => t.Code == team.Code))
            throw new InvalidOperationException($"Team with code {team.Code} already exists in this league");

        _teams.Add(team);
    }

    public override string ToString() => $"{Name} ({Code})";
}
