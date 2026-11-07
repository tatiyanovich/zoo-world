using System;
using UnityEngine;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Simulation;

namespace ZooWorld.Composition
{
  public class WorldInstaller : MonoInstaller
  {
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _animalsRoot;
    [SerializeField] private AnimalCatalog _animalCatalog;
    [SerializeField] private SimulationSettings _simulationSettings;

    public override void InstallBindings()
    {
      if (_worldCamera == null || _animalsRoot == null || _animalCatalog == null || _simulationSettings == null)
        throw new InvalidOperationException(
          "World requires a camera, an animals root, an animal catalog and simulation settings.");

      _animalCatalog.Validate();
      _simulationSettings.Validate();
      var bounds = new ArenaBounds(_worldCamera);
      foreach (var definition in _animalCatalog.Definitions)
      {
        if (!bounds.Contains(bounds.Center, definition.SpawnRadius))
          throw new InvalidOperationException($"{definition.name}: the animal must fit inside the arena.");
      }

      Container.Bind<Camera>().FromInstance(_worldCamera);
      Container.Bind<Transform>().FromInstance(_animalsRoot);
      Container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
      Container.Bind<ArenaBounds>().FromInstance(bounds);
      Container.Bind<AnimalCatalog>().FromInstance(_animalCatalog);
      Container.Bind<SimulationSettings>().FromInstance(_simulationSettings);
      Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle();
      Container.BindInterfacesAndSelfTo<AnimalSpawner>().AsSingle();
    }
  }
}
