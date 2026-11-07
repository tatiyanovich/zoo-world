using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld.Animals
{
  [CreateAssetMenu(menuName = "Zoo World/Animal Catalog")]
  public sealed class AnimalCatalog : ScriptableObject
  {
    [SerializeField] private AnimalDefinition[] _definitions = Array.Empty<AnimalDefinition>();

    public IReadOnlyList<AnimalDefinition> Definitions => _definitions;

    [ContextMenu("Validate")]
    public void Validate()
    {
      if (_definitions == null || _definitions.Length == 0)
        throw new InvalidOperationException($"{name}: at least one animal definition is required.");

      for (var index = 0; index < _definitions.Length; index++)
      {
        if (_definitions[index] == null)
          throw new InvalidOperationException($"{name}: animal definition at index {index} is missing.");

        _definitions[index].Validate();
      }
    }
  }
}
