using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Composition;
using ZooWorld.Simulation;

namespace ZooWorld.Tests
{
  public class WorldCompositionTests
  {
    private const string ScenePath = "Assets/ZooWorld/Scenes/World.unity";

    [UnityTest]
    public IEnumerator WorldInjectsSceneDependenciesAfterReload()
    {
      yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

      var firstDependencies = GetSceneDependencies();
      Assert.That(firstDependencies.Camera.orthographic, Is.True);
      Assert.That(Vector3.Dot(firstDependencies.Camera.transform.forward, Vector3.down), Is.GreaterThan(0.999f));

      var container = Object.FindFirstObjectByType<SceneContext>().Container;
      var statistics = container.Resolve<DeathStatistics>();
      var foodChain = container.Resolve<FoodChain>();
      var factory = container.Resolve<IAnimalFactory>();
      var catalog = container.Resolve<AnimalCatalog>();
      Assert.That(statistics.PreyDeaths + statistics.PredatorDeaths, Is.Zero);
      var predator = factory.Create(catalog.Definitions.First(item => item.Role == AnimalRole.Predator),
        Vector3.left * 5f, Vector3.forward);
      var prey = factory.Create(catalog.Definitions.First(item => item.Role == AnimalRole.Prey),
        Vector3.right * 5f, Vector3.forward);
      foodChain.ResolveContact(predator, prey);
      Assert.That(statistics.PreyDeaths, Is.EqualTo(1));
      Assert.That(statistics.PredatorDeaths, Is.Zero);
      predator.gameObject.SetActive(false);
      Object.Destroy(predator.gameObject);
      yield return null;
      Assert.That(statistics.PreyDeaths + statistics.PredatorDeaths, Is.EqualTo(1));

      factory.Create(catalog.Definitions.First(item => item.Role == AnimalRole.Predator),
        Vector3.left * 5f, Vector3.forward);

      yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

      var nextDependencies = GetSceneDependencies();
      Assert.That(nextDependencies.Camera, Is.Not.SameAs(firstDependencies.Camera));
      Assert.That(nextDependencies.AnimalsRoot, Is.Not.SameAs(firstDependencies.AnimalsRoot));
      Assert.That(firstDependencies.Camera == null, Is.True);
      Assert.That(firstDependencies.AnimalsRoot == null, Is.True);
      var nextStatistics = Object.FindFirstObjectByType<SceneContext>().Container.Resolve<DeathStatistics>();
      Assert.That(nextStatistics, Is.Not.SameAs(statistics));
      Assert.That(nextStatistics.PreyDeaths + nextStatistics.PredatorDeaths, Is.Zero);
      Assert.That(statistics.PreyDeaths, Is.EqualTo(1));
      Assert.That(statistics.PredatorDeaths, Is.Zero);
      LogAssert.NoUnexpectedReceived();
    }

    private static SceneDependencies GetSceneDependencies()
    {
      var scene = SceneManager.GetActiveScene();
      var roots = scene.GetRootGameObjects();
      var contexts = roots.SelectMany(root => root.GetComponentsInChildren<SceneContext>()).ToArray();
      Assert.That(contexts, Has.Length.EqualTo(1));

      var context = contexts[0];
      Assert.That(context.HasResolved, Is.True);
      Assert.That(context.Installers.Single(), Is.TypeOf<WorldInstaller>());

      var dependencies = context.Container.Instantiate<SceneDependencies>();
      Assert.That(dependencies.Camera, Is.Not.Null);
      Assert.That(dependencies.Camera.gameObject.scene, Is.EqualTo(scene));
      Assert.That(dependencies.AnimalsRoot, Is.Not.Null);
      Assert.That(dependencies.AnimalsRoot.gameObject.scene, Is.EqualTo(scene));
      Assert.That(dependencies.AnimalsRoot.name, Is.EqualTo("Animals"));
      return dependencies;
    }

    private class SceneDependencies
    {
      public Camera Camera { get; }
      public Transform AnimalsRoot { get; }

      [Inject]
      public SceneDependencies(Camera camera, Transform animalsRoot)
      {
        Camera = camera;
        AnimalsRoot = animalsRoot;
      }
    }
  }
}
