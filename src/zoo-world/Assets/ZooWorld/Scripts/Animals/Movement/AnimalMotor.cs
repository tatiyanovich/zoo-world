using System;
using UnityEngine;

namespace ZooWorld.Animals.Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Rigidbody), typeof(WanderDirection))]
  public class AnimalMotor : MonoBehaviour
  {
    [SerializeField] private float _contactRecovery = 0.3f;

    private Rigidbody _body;
    private WanderDirection _direction;
    private float _recoveryRemaining;

    public float Radius { get; private set; }

    public void ValidateConfiguration()
    {
      if (!enabled || !float.IsFinite(_contactRecovery) || _contactRecovery < 0f)
        throw new InvalidOperationException($"{name}: an enabled motor and nonnegative contact recovery are required.");

      GetComponent<WanderDirection>().ValidateConfiguration();
    }

    public Vector3 Tick(float deltaTime)
    {
      var direction = _direction.Tick(_body.position, Radius, deltaTime);
      _body.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
      _recoveryRemaining -= deltaTime;
      return direction;
    }

    public void Drive(Vector3 direction, float speed, float acceleration = float.PositiveInfinity)
    {
      if (_recoveryRemaining > 0f)
        return;

      var velocity = _body.linearVelocity;
      velocity.y = 0f;
      var requiredAcceleration = (direction * speed - velocity) / Time.fixedDeltaTime;
      _body.AddForce(Vector3.ClampMagnitude(requiredAcceleration, acceleration), ForceMode.Acceleration);
    }

    public void Stop()
    {
      _body.linearVelocity = Vector3.zero;
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

    private void OnCollisionEnter(Collision collision)
    {
      _recoveryRemaining = _contactRecovery;
    }
  }
}
