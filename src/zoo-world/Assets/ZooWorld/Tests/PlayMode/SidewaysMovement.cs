using System;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Animals.Movement;

namespace ZooWorld.Tests
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Animal), typeof(AnimalMotor))]
  public class SidewaysMovement : MonoBehaviour, IAnimalMovement
  {
    private AnimalMotor _motor;
    private Rigidbody _body;

    public void ValidateConfiguration()
    {
      if (!enabled)
        throw new InvalidOperationException($"{name}: an enabled sideways movement is required.");

      GetComponent<AnimalMotor>().ValidateConfiguration();
    }

    private void Awake()
    {
      _motor = GetComponent<AnimalMotor>();
      _body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
      var direction = _motor.Tick(Time.fixedDeltaTime);
      _motor.Drive(direction, 2f);
      _body.MoveRotation(Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up));
    }
  }
}
