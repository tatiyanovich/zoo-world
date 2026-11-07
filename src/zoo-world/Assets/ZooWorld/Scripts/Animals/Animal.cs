using System;
using UnityEngine;
using Zenject;
using ZooWorld.Animals.Movement;
using ZooWorld.Simulation;

namespace ZooWorld.Animals
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Rigidbody), typeof(Collider))]
  public class Animal : MonoBehaviour
  {
    [SerializeField] private Transform _visual;

    private Rigidbody _body;
    private FoodChain _foodChain;

    public Transform Visual => _visual;
    public AnimalDefinition Definition { get; private set; }
    public AnimalRole Role => Definition.Role;
    public long SpawnId { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsAlive { get; private set; }

    public event Action Died;
    public event Action Ate;

    [Inject]
    public void Construct(FoodChain foodChain)
    {
      _foodChain = foodChain;
    }

    public void Initialize(AnimalDefinition definition, long spawnId)
    {
      if (gameObject.activeSelf || IsInitialized)
        throw new InvalidOperationException("An animal must be inactive and unprepared before initialization.");

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

      var colliders = GetComponentsInChildren<Collider>(includeInactive: true);
      if (colliders.Length != 1 || colliders[0].transform != transform || !colliders[0].enabled || colliders[0].isTrigger)
        throw new InvalidOperationException($"{name}: one enabled non-trigger Collider on the root is required.");

      var linear = GetComponent<LinearMovement>();
      var jump = GetComponent<JumpMovement>();
      if ((linear == null) == (jump == null))
        throw new InvalidOperationException($"{name}: exactly one movement driver is required.");

      if (linear != null)
        linear.ValidateConfiguration();
      else
        jump.ValidateConfiguration();
    }

    private void Awake()
    {
      CacheBody();
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (collision.gameObject.TryGetComponent<Animal>(out var other))
        _foodChain.ResolveContact(this, other);
    }

    internal void NotifyAte()
    {
      Ate?.Invoke();
    }

    private void CacheBody()
    {
      if (_body == null)
        _body = GetComponent<Rigidbody>();
    }
  }
}
