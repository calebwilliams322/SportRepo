namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a team or individual participant
/// </summary>
public class Team
{
    public Guid Id { get; }
    public string Name { get; }
    public string Code { get; } // Short code (e.g., "LAL", "BOS", "KC")
    public string? City { get; }
    public Guid LeagueId { get; }

    // Private parameterless constructor for EF Core
    private Team()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        Code = string.Empty;
    }

    public Team(string name, string code, Guid leagueId, string? city = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Team code cannot be empty", nameof(code));

        Id = Guid.NewGuid();
        Name = name;
        Code = code.ToUpperInvariant();
        LeagueId = leagueId;
        City = city;
    }

    public string FullName => string.IsNullOrWhiteSpace(City) ? Name : $"{City} {Name}";

    public override string ToString() => FullName;
}
