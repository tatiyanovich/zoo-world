using System;
using UnityEngine;
using Zenject;
using ZooWorld.Simulation;

namespace ZooWorld.Animals.Movement
{
  [DisallowMultipleComponent]
  public sealed class WanderDirection : MonoBehaviour
  {
    [SerializeField] private float _minimumInterval = 2f;
    [SerializeField] private float _maximumInterval = 4f;
    [SerializeField] private float _returnMargin = 1f;

    private ArenaBounds _bounds;
    private IRandomSource _random;
    private float _remainingTime;
    private bool _isInitialized;

    public Vector3 Direction { get; private set; }
    public bool IsReturning { get; private set; }

    [Inject]
    public void Construct(ArenaBounds bounds, IRandomSource random)
    {
      _bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
      _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public void Initialize(Vector3 direction)
    {
      if (gameObject.activeSelf || _isInitialized)
        throw new InvalidOperationException("Initialize direction once before activating the animal.");

      direction.y = 0f;
      if (!float.IsFinite(direction.x) || !float.IsFinite(direction.z) || direction.sqrMagnitude < 0.0001f)
        throw new ArgumentException("A finite nonzero XZ direction is required.", nameof(direction));

      ValidateConfiguration();
      if (_bounds == null || _random == null)
        throw new InvalidOperationException("Inject arena bounds and randomness before initialization.");

      Direction = direction.normalized;
      _remainingTime = _random.Range(_minimumInterval, _maximumInterval);
      _isInitialized = true;
    }

    public Vector3 Tick(Vector3 position, float radius, float deltaTime)
    {
      if (!_bounds.Contains(position, radius))
        IsReturning = true;

      if (IsReturning)
      {
        Direction = _bounds.GetReturnDirection(position);
        if (!_bounds.Contains(position, radius, _returnMargin))
          return Direction;

        IsReturning = false;
        _remainingTime = _random.Range(_minimumInterval, _maximumInterval);
        return Direction;
      }

      _remainingTime -= deltaTime;
      if (_remainingTime > 0f)
        return Direction;

      var angle = _random.Range(0f, Mathf.PI * 2f);
      Direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
      _remainingTime = _random.Range(_minimumInterval, _maximumInterval);
      return Direction;
    }

    public void ValidateConfiguration()
    {
      if (!enabled || !float.IsFinite(_minimumInterval) || !float.IsFinite(_maximumInterval)
        || !float.IsFinite(_returnMargin) || _minimumInterval <= 0f || _maximumInterval < _minimumInterval || _returnMargin <= 0f)
        throw new InvalidOperationException($"{name}: positive wander intervals and return margin are required.");
    }

    private void OnEnable()
    {
      if (!_isInitialized)
        throw new InvalidOperationException($"{name}: initialize direction before activation.");
    }
  }
}
