using System;
using UnityEngine;
using ZooWorld.Animals.Movement;

namespace ZooWorld.Animals
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Rigidbody), typeof(Collider))]
  public sealed class Animal : MonoBehaviour
  {
    [SerializeField] private Transform _visual;

    private Rigidbody _body;

    public AnimalDefinition Definition { get; private set; }
    public AnimalRole Role => Definition.Role;
    public long SpawnId { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsAlive { get; private set; }

    public event Action Died;

    public void Initialize(AnimalDefinition definition, long spawnId)
    {
      if (gameObject.activeSelf || IsInitialized)
        throw new InvalidOperationException("An animal must be inactive and unprepared before initialization.");

      if (definition == null)
        throw new ArgumentNullException(nameof(definition));

      if (spawnId <= 0)
        throw new ArgumentOutOfRangeException(nameof(spawnId), "SpawnId must be positive.");

      definition.Validate();
      ValidateConfiguration();
      CacheBody();
      _body.linearVelocity = Vector3.zero;
      _body.angularVelocity = Vector3.zero;
      Definition = definition;
      SpawnId = spawnId;
      IsInitialized = true;
      IsAlive = true;
    }

    public bool TryDie()
    {
      if (!IsAlive)
        return false;

      IsAlive = false;
      gameObject.SetActive(false);
      Destroy(gameObject);
      Died?.Invoke();
      return true;
    }

    [ContextMenu("Validate")]
    public void ValidateConfiguration()
    {
      if (!enabled || _visual == null || _visual == transform || !_visual.IsChildOf(transform))
        throw new InvalidOperationException($"{name}: an enabled Animal and a separate child Visual are required.");

      var body = GetComponent<Rigidbody>();
      var constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
      if (body == null || body.isKinematic || body.useGravity || !body.detectCollisions || body.constraints != constraints)
        throw new InvalidOperationException($"{name}: a dynamic Rigidbody constrained to XZ without gravity is required.");

      if (body.mass <= 0f || float.IsNaN(body.mass) || float.IsInfinity(body.mass))
        throw new InvalidOperationException($"{name}: Rigidbody mass must be finite and positive.");

      var colliders = GetComponentsInChildren<Collider>(includeInactive: true);
      if (colliders.Length != 1 || colliders[0].transform != transform || !colliders[0].enabled || colliders[0].isTrigger)
        throw new InvalidOperationException($"{name}: one enabled non-trigger Collider on the root is required.");

      if (TryGetComponent<LinearMovement>(out var movement))
        movement.ValidateConfiguration();
    }

    private void Awake()
    {
      CacheBody();
    }

    private void OnEnable()
    {
      if (!IsInitialized || !IsAlive)
        throw new InvalidOperationException($"{name}: initialize the animal before activation.");
    }

    private void CacheBody()
    {
      if (_body == null)
        _body = GetComponent<Rigidbody>();
    }
  }
}
