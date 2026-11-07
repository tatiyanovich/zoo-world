using System;
using ZooWorld.Animals;

namespace ZooWorld.Simulation
{
  public class DeathStatistics
  {
    public int PreyDeaths { get; private set; }
    public int PredatorDeaths { get; private set; }

    public event Action Changed;

    public void RecordDeath(AnimalRole role)
    {
      if (role == AnimalRole.Prey)
        PreyDeaths++;
      else
        PredatorDeaths++;

      Changed?.Invoke();
    }
  }
}
