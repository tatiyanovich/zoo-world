using System;
using UnityEngine;

namespace ZooWorld.Simulation
{
  [CreateAssetMenu(menuName = "Zoo World/Simulation Settings")]
  public class SimulationSettings : ScriptableObject
  {
    [SerializeField] private float _minimumSpawnInterval = 1f;
    [SerializeField] private float _maximumSpawnInterval = 2f;
    [SerializeField] private int _spawnPositionAttempts = 12;

    public float MinimumSpawnInterval => _minimumSpawnInterval;
    public float MaximumSpawnInterval => _maximumSpawnInterval;
    public int SpawnPositionAttempts => _spawnPositionAttempts;

    [ContextMenu("Validate")]
    public void Validate()
    {
      if (!float.IsFinite(_minimumSpawnInterval) || !float.IsFinite(_maximumSpawnInterval)
        || _minimumSpawnInterval <= 0f || _maximumSpawnInterval < _minimumSpawnInterval || _spawnPositionAttempts <= 0)
        throw new InvalidOperationException($"{name}: positive spawn intervals and position attempts are required.");
    }
  }
}
