using ZooWorld.Animals;

namespace ZooWorld.Simulation
{
  public enum ContactVictim
  {
    None,
    First,
    Second,
  }

  public static class FoodChainRules
  {
    public static ContactVictim Resolve(AnimalRole firstRole, long firstId, AnimalRole secondRole, long secondId)
    {
      if (firstRole == AnimalRole.Prey && secondRole == AnimalRole.Prey)
        return ContactVictim.None;

      if (firstRole == AnimalRole.Prey)
        return ContactVictim.First;

      if (secondRole == AnimalRole.Prey)
        return ContactVictim.Second;

      return firstId < secondId ? ContactVictim.Second : ContactVictim.First;
    }
  }
}
