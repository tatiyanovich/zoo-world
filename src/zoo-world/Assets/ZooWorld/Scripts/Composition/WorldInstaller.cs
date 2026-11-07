using System;
using UnityEngine;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Simulation;

namespace ZooWorld.Composition
{
  public sealed class WorldInstaller : MonoInstaller
  {
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _animalsRoot;
    [SerializeField] private AnimalCatalog _animalCatalog;

    public override void InstallBindings()
    {
      if (_worldCamera == null || _animalsRoot == null)
        throw new InvalidOperationException("World requires a camera and an animals root.");

      if (_animalCatalog == null)
        throw new InvalidOperationException("World requires an animal catalog.");

      _animalCatalog.Validate();

      Container.Bind<Camera>().FromInstance(_worldCamera);
      Container.Bind<Transform>().FromInstance(_animalsRoot);
      Container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
      Container.Bind<ArenaBounds>().AsSingle();
    }
  }
}
