namespace PiratesQuest.Attributes;

/// <summary>
/// Interface for objects that can take damage.
/// </summary>
public interface IDamageable
{
  int Health { get; set; }
  int MaxHealth { get; }

  void TakeDamage(int amount)
  {
    Health -= amount;
    if (Health <= 0) OnDeath();
  }

  void OnDeath();
}