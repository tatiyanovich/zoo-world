#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Animals.Movement;
using ZooWorld.Simulation;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
  public class JumpMovementTests
  {
    private readonly List<Animal> _animals = new();
    private DiContainer _container;
    private Camera _camera;
    private ArenaBounds _bounds;
    private AnimalDefinition _definition;

    [SetUp]
    public void SetUp()
    {
      _camera = new GameObject("JumpTestCamera").AddComponent<Camera>();
      _camera.orthographic = true;
      _camera.orthographicSize = 9f;
      _camera.aspect = 16f / 9f;
      _camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
      _bounds = new ArenaBounds(_camera);
      _container = new DiContainer();
      _container.Bind<Camera>().FromInstance(_camera);
      _container.Bind<DeathStatistics>().AsSingle();
      _container.Bind<FoodChain>().AsSingle();
      _container.Bind<ArenaBounds>().FromInstance(_bounds);
      _container.Bind<IRandomSource>().FromInstance(new MinimumRandom());
      _definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/FrogDefinition.asset"));
    }

    [UnityTest]
    public IEnumerator FreeJumpsKeepDistanceIntervalAndPauseForEitherRole()
    {
      var definition = new SerializedObject(_definition);
      definition.FindProperty("_role").intValue = (int)AnimalRole.Predator;
      definition.ApplyModifiedPropertiesWithoutUndo();
      var animal = CreateAnimal(Vector3.zero, Vector3.right);
      var movement = animal.GetComponent<JumpMovement>();
      var body = animal.GetComponent<Rigidbody>();
      var origin = animal.Visual.localPosition;
      var scale = animal.Visual.localScale;
      var previousStart = 0f;
      for (var jump = 0; jump < 3; jump++)
      {
        var startPosition = body.position;
        var waitingSteps = 0;
        while (!movement.IsJumping && waitingSteps++ < 100)
        {
          Assert.That(Vector3.Distance(body.position, startPosition), Is.LessThan(0.01f));
          yield return new WaitForFixedUpdate();
        }
        Assert.That(movement.IsJumping, Is.True);
        if (jump > 0)
          Assert.That(Time.fixedTime - previousStart, Is.EqualTo(movement.Interval).Within(Time.fixedDeltaTime * 1.1f));
        previousStart = Time.fixedTime;
        var maximumHeight = 0f;
        var steps = 0;
        while (movement.IsJumping && steps++ < 100)
        {
          maximumHeight = Mathf.Max(maximumHeight, animal.Visual.localPosition.y - origin.y);
          Assert.That(body.position.y, Is.EqualTo(0f).Within(0.001f));
          yield return new WaitForFixedUpdate();
        }
        yield return new WaitForFixedUpdate();
        var distance = body.position.x - startPosition.x;
        TestContext.WriteLine($"Jump {jump + 1}: distance {distance:F5}; start {previousStart:F3}; height {maximumHeight:F3}.");
        Assert.That(distance, Is.EqualTo(movement.Distance).Within(movement.Distance * 0.05f));
        Assert.That(maximumHeight, Is.GreaterThan(0.5f));
        Assert.That(animal.Visual.localPosition, Is.EqualTo(origin));
        Assert.That(animal.Visual.localScale, Is.EqualTo(scale));
        Assert.That(animal.Role, Is.EqualTo(AnimalRole.Predator));
      }
    }

    [UnityTest]
    public IEnumerator PeacefulJumpContactPreservesPhysicalRebound()
    {
      var first = CreateAnimal(new Vector3(-1.5f, 0f, 0f), Vector3.right);
      var second = CreateAnimal(new Vector3(1.5f, 0f, 0f), Vector3.left);
      var firstBody = first.GetComponent<Rigidbody>();
      var secondBody = second.GetComponent<Rigidbody>();
      var rebounded = false;
      for (var step = 0; step < 150; step++)
      {
        yield return new WaitForFixedUpdate();
        if (firstBody.linearVelocity.x < -0.5f && secondBody.linearVelocity.x > 0.5f)
        {
          rebounded = true;
          break;
        }
      }
      Assert.That(rebounded, Is.True);
      var separation = Vector3.Distance(firstBody.position, secondBody.position);
      var velocities = new Vector2(firstBody.linearVelocity.x, secondBody.linearVelocity.x);
      for (var step = 0; step < 8; step++)
        yield return new WaitForFixedUpdate();
      var gained = Vector3.Distance(firstBody.position, secondBody.position) - separation;
      TestContext.WriteLine($"Rebound X: {velocities}; separation gained: {gained:F4}.");
      Assert.That(gained, Is.GreaterThan(0.3f));
      Assert.That(firstBody.linearVelocity.x, Is.LessThan(-0.5f));
      Assert.That(secondBody.linearVelocity.x, Is.GreaterThan(0.5f));
      Assert.That(first.IsAlive && second.IsAlive, Is.True);
    }

    [UnityTest]
    public IEnumerator ViewportChangeRedirectsCurrentJumpAndReturnsWholeBody()
    {
      var animal = CreateAnimal(new Vector3(11f, 0f, 0f), Vector3.right);
      var body = animal.GetComponent<Rigidbody>();
      var movement = animal.GetComponent<JumpMovement>();
      var motor = animal.GetComponent<AnimalMotor>();
      for (var step = 0; step < 100 && !movement.IsJumping; step++)
        yield return new WaitForFixedUpdate();
      Assert.That(movement.IsJumping, Is.True);
      _camera.aspect = 1f;
      Assert.That(_bounds.Contains(body.position, motor.Radius), Is.False);
      for (var step = 0; step < 3; step++)
        yield return new WaitForFixedUpdate();
      Assert.That(movement.IsJumping, Is.True);
      Assert.That(body.linearVelocity.x, Is.LessThan(-1f));
      var returned = false;
      for (var step = 0; step < 350; step++)
      {
        yield return new WaitForFixedUpdate();
        if (_bounds.Contains(body.position, motor.Radius, 1f))
        {
          returned = true;
          break;
        }
      }
      Assert.That(returned, Is.True);
    }

    [UnityTest]
    public IEnumerator DisableAndDeathResetVisualMotionAndJumpTimer()
    {
      var animal = CreateAnimal(Vector3.zero, Vector3.right);
      var movement = animal.GetComponent<JumpMovement>();
      var body = animal.GetComponent<Rigidbody>();
      var origin = animal.Visual.localPosition;
      var scale = animal.Visual.localScale;
      for (var step = 0; step < 100 && !movement.IsJumping; step++)
        yield return new WaitForFixedUpdate();
      Assert.That(animal.Visual.localPosition.y, Is.GreaterThan(origin.y));
      Assert.That(animal.Visual.localScale.x, Is.GreaterThan(scale.x));
      animal.gameObject.SetActive(false);
      Assert.That(animal.Visual.localPosition, Is.EqualTo(origin));
      Assert.That(animal.Visual.localScale, Is.EqualTo(scale));
      Assert.That(movement.IsJumping, Is.False);
      Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
      animal.gameObject.SetActive(true);
      var position = body.position;
      for (var step = 0; step < 30; step++)
        yield return new WaitForFixedUpdate();
      Assert.That(movement.IsJumping, Is.False);
      Assert.That(Vector3.Distance(body.position, position), Is.LessThan(0.01f));
      for (var step = 0; step < 100 && !movement.IsJumping; step++)
        yield return new WaitForFixedUpdate();
      Assert.That(movement.IsJumping, Is.True);
      Assert.That(animal.TryDie(), Is.True);
      Assert.That(animal.Visual.localPosition, Is.EqualTo(origin));
      Assert.That(animal.Visual.localScale, Is.EqualTo(scale));
      Assert.That(movement.IsJumping, Is.False);
      Assert.That(animal.gameObject.activeSelf, Is.False);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
      foreach (var animal in _animals)
      {
        if (animal != null)
          Object.Destroy(animal.gameObject);
      }
      _animals.Clear();
      Object.Destroy(_camera.gameObject);
      Object.Destroy(_definition);
      yield return null;
      LogAssert.NoUnexpectedReceived();
    }

    private Animal CreateAnimal(Vector3 position, Vector3 direction)
    {
      var animal = _container.InstantiatePrefabForComponent<Animal>(_definition.Prefab);
      _animals.Add(animal);
      var wander = animal.GetComponent<WanderDirection>();
      var serializedWander = new SerializedObject(wander);
      serializedWander.FindProperty("_minimumInterval").floatValue = 60f;
      serializedWander.FindProperty("_maximumInterval").floatValue = 60f;
      serializedWander.ApplyModifiedPropertiesWithoutUndo();
      animal.Initialize(_definition, _animals.Count);
      animal.transform.position = position;
      wander.Initialize(direction);
      animal.gameObject.SetActive(true);
      return animal;
    }

    private class MinimumRandom : IRandomSource
    {
      public float Range(float minimum, float maximum) => minimum;

      public int Range(int minimum, int maximum) => minimum;
    }
  }
}
#endif
