using NUnit.Framework;
using ZooWorld.Animals;
using ZooWorld.Simulation;

namespace ZooWorld.Tests
{
  public class FoodChainRulesTests
  {
    [TestCase(AnimalRole.Prey, 1, AnimalRole.Prey, 2, ContactVictim.None)]
    [TestCase(AnimalRole.Predator, 1, AnimalRole.Prey, 2, ContactVictim.Second)]
    [TestCase(AnimalRole.Predator, 1, AnimalRole.Predator, 2, ContactVictim.Second)]
    public void RolePairsKeepTheSameVictimWhenSwapped(
      AnimalRole firstRole, long firstId, AnimalRole secondRole, long secondId, ContactVictim expected)
    {
      Assert.That(FoodChainRules.Resolve(firstRole, firstId, secondRole, secondId), Is.EqualTo(expected));
      var reversed = expected;
      if (expected != ContactVictim.None)
        reversed = expected == ContactVictim.First ? ContactVictim.Second : ContactVictim.First;
      Assert.That(FoodChainRules.Resolve(secondRole, secondId, firstRole, firstId), Is.EqualTo(reversed));
    }
  }
}
