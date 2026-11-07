using System;
using UnityEngine;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Simulation;
using ZooWorld.UI;

namespace ZooWorld.Composition
{
  public class WorldInstaller : MonoInstaller
  {
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _animalsRoot;
    [SerializeField] private AnimalCatalog _animalCatalog;
    [SerializeField] private SimulationSettings _simulationSettings;
    [SerializeField] private DeathCounterView _deathCounterView;

    public override void InstallBindings()
    {
      if (_worldCamera == null || _animalsRoot == null || _animalCatalog == null ||
        _simulationSettings == null || _deathCounterView == null)
        throw new InvalidOperationException(
          "World requires a camera, an animals root, an animal catalog, simulation settings and death counters.");

      _animalCatalog.Validate();
      _simulationSettings.Validate();
      _deathCounterView.ValidateConfiguration();
      var bounds = new ArenaBounds(_worldCamera);
      foreach (var definition in _animalCatalog.Definitions)
      {
        if (!bounds.Contains(bounds.Center, definition.SpawnRadius))
          throw new InvalidOperationException($"{definition.name}: the animal must fit inside the arena.");

        if (definition.Prefab.TryGetComponent<TastyLabel>(out var label))
          label.ValidateConfiguration();
      }

      Container.Bind<Camera>().FromInstance(_worldCamera);
      Container.Bind<Transform>().FromInstance(_animalsRoot);
      Container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
      Container.Bind<ArenaBounds>().FromInstance(bounds);
      Container.Bind<AnimalCatalog>().FromInstance(_animalCatalog);
      Container.Bind<SimulationSettings>().FromInstance(_simulationSettings);
      Container.Bind<DeathStatistics>().AsSingle();
      Container.Bind<FoodChain>().AsSingle();
      Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle();
      Container.BindInterfacesAndSelfTo<AnimalSpawner>().AsSingle();
    }
  }
}
