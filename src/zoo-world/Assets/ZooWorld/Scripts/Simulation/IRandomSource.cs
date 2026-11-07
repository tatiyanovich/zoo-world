namespace ZooWorld.Simulation
{
  public interface IRandomSource
  {
    float Range(float minimum, float maximum);
    int Range(int minimum, int maximum);
  }
}
