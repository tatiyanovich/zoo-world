using UnityEngine;
using Zenject;
using ZooWorld.Animals.Movement;

namespace ZooWorld.Animals
{
  public class AnimalFactory : IAnimalFactory
  {
    private readonly DiContainer _container;
    private readonly Transform _animalsRoot;
    private long _lastSpawnId;

    public AnimalFactory(DiContainer container, Transform animalsRoot)
    {
      _container = container;
      _animalsRoot = animalsRoot;
    }

    public Animal Create(AnimalDefinition definition, Vector3 position, Vector3 direction)
    {
      var rotation = Quaternion.LookRotation(direction, Vector3.up);
      var animal = _container.InstantiatePrefabForComponent<Animal>(definition.Prefab, position, rotation, _animalsRoot);
      animal.Initialize(definition, checked(++_lastSpawnId));
      animal.GetComponent<WanderDirection>().Initialize(direction);
      animal.gameObject.SetActive(true);
      return animal;
    }
  }
}
