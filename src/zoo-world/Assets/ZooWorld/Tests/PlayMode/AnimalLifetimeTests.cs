#if UNITY_EDITOR
using System;
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
  public class AnimalLifetimeTests
  {
    private const string SettingsPath = "Assets/ZooWorld/Settings/Animals/";

    private readonly List<Animal> _animals = new();
    private DiContainer _container;
    private Camera _camera;

    [SetUp]
    public void SetUp()
    {
      _camera = new GameObject("LifetimeTestCamera").AddComponent<Camera>();
      _camera.orthographic = true;
      _camera.orthographicSize = 9f;
      _camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
      _container = new DiContainer();
      _container.Bind<Camera>().FromInstance(_camera);
      _container.Bind<DeathStatistics>().AsSingle();
      _container.Bind<FoodChain>().AsSingle();
      _container.Bind<ArenaBounds>().FromInstance(new ArenaBounds(_camera));
      _container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
    }

    [UnityTest]
    public IEnumerator SharedDefinitionKeepsIndependentLifetime()
    {
      var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(SettingsPath + "AnimalCatalog.asset");
      foreach (var definition in catalog.Definitions)
      {
        var savedDefinition = EditorJsonUtility.ToJson(definition);
        var first = CreateAnimal(definition, 1);
        var second = CreateAnimal(definition, 2);
        second.transform.position = Vector3.right * 3f;
        first.gameObject.SetActive(true);
        second.gameObject.SetActive(true);
        yield return null;

        var deathEvents = 0;
        first.Died += () =>
        {
          deathEvents++;
          Assert.That(first.IsAlive, Is.False);
          Assert.That(first.gameObject.activeSelf, Is.False);
          Assert.That(first.TryDie(), Is.False);
        };

        Assert.That(first.Definition, Is.SameAs(second.Definition));
        Assert.That(first.SpawnId, Is.Not.EqualTo(second.SpawnId));
        Assert.That(first.TryDie(), Is.True);
        Assert.That(first.TryDie(), Is.False);
        Assert.That(deathEvents, Is.EqualTo(1));
        Assert.That(second.IsAlive, Is.True);
        Assert.That(second.gameObject.activeInHierarchy, Is.True);
        Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(savedDefinition));
        yield return null;

        Assert.That(first == null, Is.True);
      }
      LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator FactoryActivatesBothPrefabsWithUniqueIdsAndMovement()
    {
      var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(SettingsPath + "AnimalCatalog.asset");
      var factory = new AnimalFactory(_container, _camera.transform);
      var positions = new List<Vector3>();
      foreach (var definition in catalog.Definitions)
      {
        var position = new Vector3(_animals.Count * 10f - 5f, 0f, 0f);
        var animal = factory.Create(definition, position, Vector3.forward);
        _animals.Add(animal);
        positions.Add(position);
        Assert.That(animal.Definition, Is.SameAs(definition));
        Assert.That(animal.SpawnId, Is.EqualTo(_animals.Count));
        Assert.That(animal.IsInitialized && animal.IsAlive && animal.gameObject.activeInHierarchy, Is.True);
        Assert.That(animal.transform.parent, Is.SameAs(_camera.transform));
        Assert.That(Vector3.Distance(animal.GetComponent<Rigidbody>().position, position), Is.LessThan(0.0001f));
        Assert.That(animal.GetComponent<WanderDirection>().Direction, Is.EqualTo(Vector3.forward));
      }

      for (var step = 0; step < 75; step++)
        yield return new WaitForFixedUpdate();

      for (var index = 0; index < _animals.Count; index++)
        Assert.That(Vector3.Distance(_animals[index].GetComponent<Rigidbody>().position, positions[index]), Is.GreaterThan(0.5f));

      LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void InitializationPreparesBodyAndCannotRepeat()
    {
      var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(SettingsPath + "FrogDefinition.asset");
      var animal = _container.InstantiatePrefabForComponent<Animal>(definition.Prefab);
      _animals.Add(animal);
      Assert.That(animal.gameObject.activeSelf, Is.False);
      Assert.That(animal.IsInitialized, Is.False);
      Assert.That(animal.TryDie(), Is.False);

      var body = animal.GetComponent<Rigidbody>();
      body.linearVelocity = Vector3.one;
      body.angularVelocity = Vector3.one;
      animal.Initialize(definition, 1);
      Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
      Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
      Assert.Throws<InvalidOperationException>(() => animal.Initialize(definition, 2));
      animal.gameObject.SetActive(true);
      Assert.Throws<InvalidOperationException>(() => animal.Initialize(definition, 2));
      LogAssert.NoUnexpectedReceived();
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
      yield return null;
    }

    private Animal CreateAnimal(AnimalDefinition definition, long spawnId)
    {
      var animal = _container.InstantiatePrefabForComponent<Animal>(definition.Prefab);
      _animals.Add(animal);
      Assert.That(animal.gameObject.activeSelf, Is.False);
      Assert.That(animal.IsInitialized, Is.False);
      animal.Initialize(definition, spawnId);
      if (animal.TryGetComponent<WanderDirection>(out var wander))
        wander.Initialize(Vector3.forward);
      return animal;
    }
  }
}
#endif
