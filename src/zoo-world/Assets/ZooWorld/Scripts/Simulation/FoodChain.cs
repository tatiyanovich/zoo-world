using ZooWorld.Animals;

namespace ZooWorld.Simulation
{
  public class FoodChain
  {
    private readonly DeathStatistics _statistics;

    public FoodChain(DeathStatistics statistics)
    {
      _statistics = statistics;
    }

    public void ResolveContact(Animal first, Animal second)
    {
      if (!first.IsAlive || !second.IsAlive)
        return;

      var result = FoodChainRules.Resolve(first.Role, first.SpawnId, second.Role, second.SpawnId);
      if (result == ContactVictim.None)
        return;

      var victim = result == ContactVictim.First ? first : second;
      var predator = result == ContactVictim.First ? second : first;
      if (!victim.TryDie())
        return;

      _statistics.RecordDeath(victim.Role);
      if (predator.IsAlive)
        predator.NotifyAte();
    }
  }
}
