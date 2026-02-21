namespace PiratesQuest;

using Godot;

public partial class Identity : Node
{
  private string _playerName = "";

  // Pirate-themed name prefixes (adjectives/titles)
  // These describe the pirate's personality or reputation
  private static readonly string[] NamePrefixes = [
    "Captain", "Salty", "One-Eyed", "Scurvy", "Barnacle",
    "Rusty", "Stormy", "Crusty", "Jolly", "Dread",
    "Red", "Black", "Lucky", "Mad", "Peg-Leg"
  ];

  // Pirate-themed name suffixes (nicknames)
  // These are the actual "name" part
  private static readonly string[] NameSuffixes = [
    "Jack", "Pete", "Morgan", "Bones", "Sparrow",
    "Hook", "Beard", "Silver", "Flint", "Anne",
    "Kidd", "Drake", "Rackham", "Vane", "Bellamy"
  ];

  public override void _Ready()
  {
    GD.Print("Identity ready");
  }

  public string PlayerName
  {
    // When getting the name, if it's empty or whitespace, generate a random pirate name
    get
    {
      // string.IsNullOrWhiteSpace() checks for null, empty, or whitespace-only strings
      // This is a handy C# utility for input validation
      if (string.IsNullOrWhiteSpace(_playerName))
      {
        return GenerateRandomPirateName();
      }
      return _playerName;
    }
    set => _playerName = value;
  }

  /// <summary>
  /// Generates a random pirate-themed name by combining a prefix and suffix.
  /// Uses GD.Randi() which is Godot's random integer generator.
  /// </summary>
  /// <returns>A string like "Captain Jack" or "Salty Bones"</returns>
  private static string GenerateRandomPirateName()
  {
    // GD.Randi() returns a random unsigned 32-bit integer
    // The % operator (modulo) limits it to our array size
    // This gives us a random index: 0 to array.Length-1
    string prefix = NamePrefixes[GD.Randi() % NamePrefixes.Length];
    string suffix = NameSuffixes[GD.Randi() % NameSuffixes.Length];

    // String interpolation with $ combines strings nicely
    return $"{prefix} {suffix}";
  }
}