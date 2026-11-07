using System;
using UnityEngine;
using Zenject;

namespace ZooWorld.Composition
{
  public sealed class WorldInstaller : MonoInstaller
  {
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _animalsRoot;

    public override void InstallBindings()
    {
      if (_worldCamera == null || _animalsRoot == null)
        throw new InvalidOperationException("World requires a camera and an animals root.");

      Container.Bind<Camera>().FromInstance(_worldCamera);
      Container.Bind<Transform>().FromInstance(_animalsRoot);
    }
  }
}
