using System;
using UnityEngine;

namespace ZooWorld.Animals
{
  [CreateAssetMenu(menuName = "Zoo World/Animal Definition")]
  public class AnimalDefinition : ScriptableObject
  {
    [SerializeField] private AnimalRole _role;
    [SerializeField] private Animal _prefab;

    public AnimalRole Role => _role;
    public Animal Prefab => _prefab;
    public float SpawnRadius { get; private set; }

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
      SpawnRadius = CalculateSpawnRadius();
    }

    private float CalculateSpawnRadius()
    {
      var collider = _prefab.GetComponent<Collider>();
      Vector3 center;
      float radius;
      if (collider is BoxCollider box)
      {
        center = box.center;
        radius = box.size.magnitude * 0.5f;
      }
      else if (collider is CapsuleCollider capsule)
      {
        center = capsule.center;
        radius = Mathf.Max(capsule.radius, capsule.height * 0.5f);
      }
      else if (collider is SphereCollider sphere)
      {
        center = sphere.center;
        radius = sphere.radius;
      }
      else
      {
        throw new InvalidOperationException($"{name}: spawning requires a Box, Capsule or Sphere Collider.");
      }

      var scale = _prefab.transform.lossyScale;
      var maximumScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
      return (center.magnitude + radius) * maximumScale;
    }
  }
}
