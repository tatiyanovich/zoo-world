using System;
using UnityEngine;

namespace ZooWorld.Animals.Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Animal), typeof(WanderDirection))]
  public class LinearMovement : MonoBehaviour
  {
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _acceleration = 8f;
    [SerializeField] private float _contactRecovery = 0.3f;

    private Rigidbody _body;
    private WanderDirection _direction;
    private float _recoveryRemaining;

    public float Speed => _speed;
    public float Radius { get; private set; }

    public void ValidateConfiguration()
    {
      if (!enabled || !float.IsFinite(_speed) || !float.IsFinite(_acceleration) || !float.IsFinite(_contactRecovery)
        || _speed <= 0f || _acceleration <= 0f || _contactRecovery < 0f)
        throw new InvalidOperationException($"{name}: positive speed, acceleration and nonnegative contact recovery are required.");

      GetComponent<WanderDirection>().ValidateConfiguration();
    }

    private void Awake()
    {
      _body = GetComponent<Rigidbody>();
      _direction = GetComponent<WanderDirection>();
    }

    private void OnEnable()
    {
      _recoveryRemaining = 0f;
      var bodyBounds = GetComponent<Collider>().bounds;
      var extents = bodyBounds.extents;
      var offset = bodyBounds.center - _body.position;
      Radius = new Vector2(extents.x, extents.z).magnitude + new Vector2(offset.x, offset.z).magnitude;
    }

    private void FixedUpdate()
    {
      var direction = _direction.Tick(_body.position, Radius, Time.fixedDeltaTime);
      _body.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
      if (_recoveryRemaining > 0f)
      {
        _recoveryRemaining -= Time.fixedDeltaTime;
        return;
      }

      var velocity = _body.linearVelocity;
      velocity.y = 0f;
      var acceleration = (direction * _speed - velocity) / Time.fixedDeltaTime;
      _body.AddForce(Vector3.ClampMagnitude(acceleration, _acceleration), ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
      _recoveryRemaining = _contactRecovery;
    }
  }
}
