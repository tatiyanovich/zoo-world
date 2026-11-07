using System;
using UnityEngine;

namespace ZooWorld.Animals.Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Animal), typeof(AnimalMotor))]
  public class LinearMovement : MonoBehaviour, IAnimalMovement
  {
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _acceleration = 8f;
    private AnimalMotor _motor;

    public float Speed => _speed;
    public float Radius => _motor.Radius;

    public void ValidateConfiguration()
    {
      if (!enabled || !float.IsFinite(_speed) || !float.IsFinite(_acceleration) || _speed <= 0f || _acceleration <= 0f)
        throw new InvalidOperationException($"{name}: positive speed and acceleration are required.");

      GetComponent<AnimalMotor>().ValidateConfiguration();
    }

    private void Awake()
    {
      _motor = GetComponent<AnimalMotor>();
    }

    private void FixedUpdate()
    {
      var direction = _motor.Tick(Time.fixedDeltaTime);
      _motor.Drive(direction, _speed, _acceleration);
    }
  }
}
