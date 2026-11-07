using System;
using UnityEngine;

namespace ZooWorld.Animals
{
  [CreateAssetMenu(menuName = "Zoo World/Animal Definition")]
  public sealed class AnimalDefinition : ScriptableObject
  {
    [SerializeField] private AnimalRole _role;
    [SerializeField] private Animal _prefab;

    public AnimalRole Role => _role;
    public Animal Prefab => _prefab;

    [ContextMenu("Validate")]
    public void Validate()
    {
      if (_role != AnimalRole.Prey && _role != AnimalRole.Predator)
        throw new InvalidOperationException($"{name}: unknown animal role.");

      if (_prefab == null)
        throw new InvalidOperationException($"{name}: an animal prefab is required.");

      if (_prefab.transform.parent != null || _prefab.gameObject.activeSelf)
        throw new InvalidOperationException($"{name}: the animal prefab must have an inactive root.");

      _prefab.ValidateConfiguration();
    }
  }
}
