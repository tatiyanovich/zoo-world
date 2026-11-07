using UnityEngine;

namespace ZooWorld.Animals
{
  public interface IAnimalFactory
  {
    Animal Create(AnimalDefinition definition, Vector3 position, Vector3 direction);
  }
}
