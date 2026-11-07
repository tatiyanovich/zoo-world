using UnityEngine;

namespace ZooWorld.Simulation
{
  public class UnityRandomSource : IRandomSource
  {
    public float Range(float minimum, float maximum) => Random.Range(minimum, maximum);

    public int Range(int minimum, int maximum) => Random.Range(minimum, maximum);
  }
}
