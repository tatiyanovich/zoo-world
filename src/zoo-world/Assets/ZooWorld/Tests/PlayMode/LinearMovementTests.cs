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
  public class LinearMovementTests
  {
    private readonly List<Animal> _animals = new();
    private DiContainer _container;
    private Camera _camera;
    private ArenaBounds _bounds;
    private AnimalDefinition _definition;

    [SetUp]
    public void SetUp()
    {
      _camera = new GameObject("MovementTestCamera").AddComponent<Camera>();
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
        "Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset"));
      var serializedDefinition = new SerializedObject(_definition);
      serializedDefinition.FindProperty("_role").intValue = (int)AnimalRole.Prey;
      serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
    }

    [UnityTest]
    public IEnumerator FreeMovementMatchesSpeedAfterAcceleration()
    {
      var animal = CreateAnimal(Vector3.zero, Vector3.right);
      var body = animal.GetComponent<Rigidbody>();
      for (var step = 0; step < 25; step++)
        yield return new WaitForFixedUpdate();

      var start = body.position;
      const int MeasuredSteps = 100;
      for (var step = 0; step < MeasuredSteps; step++)
        yield return new WaitForFixedUpdate();

      var expected = animal.GetComponent<LinearMovement>().Speed * MeasuredSteps * Time.fixedDeltaTime;
      var distance = body.position.x - start.x;
      TestContext.WriteLine($"Free distance: {distance:F5}; expected: {expected:F5}; tolerance: 5% after 0.5 s warmup.");
      Assert.That(distance, Is.EqualTo(expected).Within(expected * 0.05f));
      Assert.That(Mathf.Abs(body.position.z), Is.LessThan(0.01f));
      Assert.That(Vector3.Dot(body.rotation * Vector3.forward, Vector3.right), Is.GreaterThan(0.999f));
      Assert.That(animal.IsAlive, Is.True);
    }

    [UnityTest]
    public IEnumerator PeacefulContactPreservesReboundThenResumesMovement()
    {
      var first = CreateAnimal(new Vector3(-2f, 0f, 0f), Vector3.right);
      var second = CreateAnimal(new Vector3(2f, 0f, 0f), Vector3.left);
      var firstBody = first.GetComponent<Rigidbody>();
      var secondBody = second.GetComponent<Rigidbody>();
      TestContext.WriteLine($"Initial positions: {firstBody.position}, {secondBody.position}.");
      Assert.That(firstBody.position.x, Is.EqualTo(-2f).Within(0.001f));
      Assert.That(secondBody.position.x, Is.EqualTo(2f).Within(0.001f));
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
      Assert.That(rebounded, Is.True, "Both peaceful bodies must receive a physical rebound.");
      var firstVelocity = firstBody.linearVelocity.x;
      var secondVelocity = secondBody.linearVelocity.x;
      var contactDistance = Vector3.Distance(firstBody.position, secondBody.position);
      yield return new WaitForFixedUpdate();
      Assert.That(firstBody.linearVelocity.x, Is.LessThan(-0.5f));
      Assert.That(secondBody.linearVelocity.x, Is.GreaterThan(0.5f));
      for (var step = 0; step < 8; step++)
        yield return new WaitForFixedUpdate();

      var separation = Vector3.Distance(firstBody.position, secondBody.position) - contactDistance;
      TestContext.WriteLine($"Rebound X: {firstVelocity:F4}, {secondVelocity:F4}; separation gained in 9 steps: {separation:F4}.");
      Assert.That(separation, Is.GreaterThan(0.15f));
      Assert.That(first.IsAlive && second.IsAlive, Is.True);
      Assert.That(first.Role, Is.EqualTo(AnimalRole.Prey));
      Assert.That(second.Role, Is.EqualTo(AnimalRole.Prey));

      for (var step = 0; step < 30; step++)
        yield return new WaitForFixedUpdate();
      TestContext.WriteLine($"Recovered X: {firstBody.linearVelocity.x:F4}, {secondBody.linearVelocity.x:F4}.");
      Assert.That(firstBody.linearVelocity.x, Is.GreaterThan(1f));
      Assert.That(secondBody.linearVelocity.x, Is.LessThan(-1f));
    }

    [UnityTest]
    public IEnumerator NarrowedViewportReturnsAnAlreadyMovingBody()
    {
      var animal = CreateAnimal(new Vector3(14f, 0f, 0f), Vector3.right);
      var body = animal.GetComponent<Rigidbody>();
      var movement = animal.GetComponent<LinearMovement>();
      for (var step = 0; step < 10; step++)
        yield return new WaitForFixedUpdate();

      Assert.That(_bounds.Contains(body.position, movement.Radius), Is.True);
      _camera.aspect = 4f / 3f;
      Assert.That(_bounds.Contains(body.position, movement.Radius), Is.False);

      var returned = false;
      for (var step = 0; step < 250; step++)
      {
        yield return new WaitForFixedUpdate();
        if (_bounds.Contains(body.position, movement.Radius, 1f))
        {
          returned = true;
          break;
        }
      }
      Assert.That(returned, Is.True);
      Assert.That(body.linearVelocity.x, Is.LessThan(-1f));
      Assert.That(animal.IsAlive, Is.True);
    }

    [UnityTest]
    public IEnumerator EverySideAndCornerReturnsAtWideAndStandardAspect()
    {
      foreach (var aspect in new[] { 16f / 9f, 4f / 3f })
      {
        _camera.aspect = aspect;
        var halfWidth = _camera.orthographicSize * aspect;
        for (var x = -1; x <= 1; x++)
        {
          for (var z = -1; z <= 1; z++)
          {
            if (x == 0 && z == 0)
              continue;
            var position = new Vector3(x * (halfWidth + 1f), 0f, z * 10f);
            var animal = CreateAnimal(position, position.normalized);
            Assert.That(Vector3.Distance(animal.GetComponent<Rigidbody>().position, position), Is.LessThan(0.001f));
            Assert.That(_bounds.Contains(position, animal.GetComponent<LinearMovement>().Radius), Is.False);
          }
        }

        var returned = false;
        for (var step = 0; step < 500; step++)
        {
          yield return new WaitForFixedUpdate();
          returned = true;
          foreach (var animal in _animals)
          {
            var movement = animal.GetComponent<LinearMovement>();
            returned &= _bounds.Contains(animal.GetComponent<Rigidbody>().position, movement.Radius, 0.5f);
            Assert.That(animal.IsAlive, Is.True);
          }
          if (returned)
            break;
        }
        Assert.That(returned, Is.True, $"All eight bodies must return at aspect {aspect}.");
        foreach (var animal in _animals)
          Object.Destroy(animal.gameObject);
        _animals.Clear();
        yield return null;
      }
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
