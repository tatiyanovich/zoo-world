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
  public class FoodChainTests
  {
    private readonly List<Animal> _animals = new();
    private Camera _camera;
    private AnimalFactory _factory;
    private FoodChain _foodChain;
    private DeathStatistics _statistics;
    private AnimalDefinition _prey;
    private AnimalDefinition _predator;

    [SetUp]
    public void SetUp()
    {
      _camera = new GameObject("FoodChainTestCamera").AddComponent<Camera>();
      _camera.orthographic = true;
      _camera.orthographicSize = 9f;
      _camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
      _statistics = new DeathStatistics();
      _foodChain = new FoodChain(_statistics);
      var container = new DiContainer();
      container.Bind<Camera>().FromInstance(_camera);
      container.Bind<FoodChain>().FromInstance(_foodChain);
      container.Bind<ArenaBounds>().FromInstance(new ArenaBounds(_camera));
      container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
      _factory = new AnimalFactory(container, _camera.transform);
      _prey = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Settings/Animals/FrogDefinition.asset");
      _predator = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset");
    }

    [UnityTest]
    public IEnumerator RealCollisionsResolveMixedAndPredatorPairsInEitherCreationOrder()
    {
      for (var pair = 0; pair < 3; pair++)
      {
        for (var order = 0; order < 2; order++)
        {
          var firstDefinition = pair == 0 ? _prey : _predator;
          var secondDefinition = pair == 1 ? _prey : _predator;
          Animal first;
          Animal second;
          if (order == 0)
          {
            first = CreateAnimal(firstDefinition, Vector3.left * 2f);
            second = CreateAnimal(secondDefinition, Vector3.right * 2f);
          }
          else
          {
            second = CreateAnimal(secondDefinition, Vector3.right * 2f);
            first = CreateAnimal(firstDefinition, Vector3.left * 2f);
          }

          var victim = first.Role == AnimalRole.Prey || (pair == 2 && first.SpawnId > second.SpawnId) ? first : second;
          var survivor = victim == first ? second : first;
          var preyBefore = _statistics.PreyDeaths;
          var predatorBefore = _statistics.PredatorDeaths;
          var deaths = 0;
          var meals = 0;
          var victimMeals = 0;
          victim.Ate += () => victimMeals++;
          survivor.Ate += () => meals++;
          victim.Died += () =>
          {
            deaths++;
            Assert.That(victim.IsAlive || victim.gameObject.activeSelf || victim.gameObject.activeInHierarchy, Is.False);
            Assert.That(victim.TryDie(), Is.False);
            _foodChain.ResolveContact(victim, survivor);
            _foodChain.ResolveContact(survivor, victim);
          };
          first.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 2f;
          second.GetComponent<Rigidbody>().linearVelocity = Vector3.left * 2f;
          for (var step = 0; step < 100 && deaths == 0; step++)
            yield return new WaitForFixedUpdate();

          Assert.That(deaths, Is.EqualTo(1), $"Pair {pair}, creation order {order} must collide.");
          Assert.That(meals, Is.EqualTo(1));
          Assert.That(victimMeals, Is.Zero);
          Assert.That(survivor.IsAlive && survivor.gameObject.activeInHierarchy, Is.True);
          Assert.That(_statistics.PreyDeaths - preyBefore, Is.EqualTo(pair == 2 ? 0 : 1));
          Assert.That(_statistics.PredatorDeaths - predatorBefore, Is.EqualTo(pair == 2 ? 1 : 0));
          TestContext.WriteLine($"Pair {pair}, creation order {order}: one death, one meal, survivor ID {survivor.SpawnId}.");
          Object.Destroy(survivor.gameObject);
          yield return null;
        }
      }
    }

    [Test]
    public void RepeatedAndCompetingContactsCountEachVictimOnce()
    {
      var older = CreateAnimal(_predator, Vector3.left * 5f);
      var younger = CreateAnimal(_predator, Vector3.right * 5f);
      var prey = CreateAnimal(_prey, Vector3.zero);
      var olderMeals = 0;
      var youngerMeals = 0;
      var changes = 0;
      older.Ate += () => olderMeals++;
      younger.Ate += () => youngerMeals++;
      _statistics.Changed += () => changes++;
      _foodChain.ResolveContact(younger, prey);
      _foodChain.ResolveContact(older, prey);
      _foodChain.ResolveContact(prey, younger);
      _foodChain.ResolveContact(younger, older);
      _foodChain.ResolveContact(older, younger);
      var untouched = CreateAnimal(_prey, Vector3.forward * 5f);
      _foodChain.ResolveContact(younger, untouched);
      Assert.That(prey.IsAlive || prey.gameObject.activeSelf, Is.False);
      Assert.That(younger.IsAlive || younger.gameObject.activeSelf, Is.False);
      Assert.That(older.IsAlive && untouched.IsAlive, Is.True);
      Assert.That(_statistics.PreyDeaths, Is.EqualTo(1));
      Assert.That(_statistics.PredatorDeaths, Is.EqualTo(1));
      Assert.That(changes, Is.EqualTo(2));
      Assert.That(olderMeals, Is.EqualTo(1));
      Assert.That(youngerMeals, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator SimultaneousPhysicalContactsDoNotDuplicateDeathsOrMeals()
    {
      var older = CreateAnimal(_predator, new Vector3(-0.15f, 0f, 0f));
      var younger = CreateAnimal(_predator, new Vector3(0.15f, 0f, 0f));
      var prey = CreateAnimal(_prey, Vector3.zero);
      var deaths = 0;
      var meals = 0;
      older.Ate += () => meals++;
      younger.Ate += () => meals++;
      younger.Died += () => deaths++;
      prey.Died += () => deaths++;
      for (var step = 0; step < 10 && deaths < 2; step++)
        yield return new WaitForFixedUpdate();

      Assert.That(older.IsAlive, Is.True);
      Assert.That(deaths, Is.EqualTo(2));
      Assert.That(meals, Is.EqualTo(2));
      Assert.That(_statistics.PreyDeaths, Is.EqualTo(1));
      Assert.That(_statistics.PredatorDeaths, Is.EqualTo(1));
      yield return null;
      Assert.That(younger == null && prey == null, Is.True);
      Object.Destroy(older.gameObject);
      yield return null;
      Assert.That(_statistics.PreyDeaths + _statistics.PredatorDeaths, Is.EqualTo(2));
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
      LogAssert.NoUnexpectedReceived();
    }

    private Animal CreateAnimal(AnimalDefinition definition, Vector3 position)
    {
      var animal = _factory.Create(definition, position, Vector3.forward);
      _animals.Add(animal);
      if (animal.TryGetComponent<LinearMovement>(out var linear))
        linear.enabled = false;
      if (animal.TryGetComponent<JumpMovement>(out var jump))
        jump.enabled = false;
      return animal;
    }
  }
}
#endif
