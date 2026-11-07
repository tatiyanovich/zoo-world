#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Animals.Movement;
using ZooWorld.Composition;
using ZooWorld.Simulation;
using ZooWorld.UI;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
  public class AnimalCompositionTests
  {
    private GameObject _root;
    private Animal _template;
    private AnimalDefinition _predator;
    private AnimalDefinition _prey;
    private AnimalCatalog _catalog;
    private Scene _worldScene;

    [UnityTest]
    public IEnumerator ThirdMovementUsesExistingInstallerAndFactory()
    {
      yield return SceneManager.LoadSceneAsync("Assets/ZooWorld/Scenes/World.unity");
      _worldScene = SceneManager.GetActiveScene();
      _root = new GameObject("ThirdMovementTest");
      _root.SetActive(false);
      var source = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset");
      _template = Object.Instantiate(source.Prefab);
      Object.DestroyImmediate(_template.GetComponent<LinearMovement>());
      _template.gameObject.AddComponent<SidewaysMovement>();
      _predator = Object.Instantiate(source);
      var definitionSettings = new SerializedObject(_predator);
      definitionSettings.FindProperty("_prefab").objectReferenceValue = _template;
      definitionSettings.ApplyModifiedPropertiesWithoutUndo();
      _catalog = ScriptableObject.CreateInstance<AnimalCatalog>();
      var catalogSettings = new SerializedObject(_catalog);
      var definitions = catalogSettings.FindProperty("_definitions");
      definitions.arraySize = 1;
      definitions.GetArrayElementAtIndex(0).objectReferenceValue = _predator;
      catalogSettings.ApplyModifiedPropertiesWithoutUndo();
      var installer = _root.AddComponent<WorldInstaller>();
      EditorUtility.CopySerialized(Object.FindFirstObjectByType<WorldInstaller>(), installer);
      var installerSettings = new SerializedObject(installer);
      installerSettings.FindProperty("_animalCatalog").objectReferenceValue = _catalog;
      installerSettings.FindProperty("_animalsRoot").objectReferenceValue = _root.transform;
      installerSettings.ApplyModifiedPropertiesWithoutUndo();
      var container = new DiContainer();
      container.Inject(installer);
      installer.InstallBindings();
      _root.SetActive(true);
      var factory = container.Resolve<IAnimalFactory>();
      var animal = factory.Create(container.Resolve<AnimalCatalog>().Definitions[0], Vector3.zero, Vector3.forward);
      Assert.That(animal.GetComponents<IAnimalMovement>(), Has.Length.EqualTo(1));
      Assert.That(animal.GetComponent<SidewaysMovement>(), Is.Not.Null);
      Assert.That(animal.IsInitialized && animal.IsAlive, Is.True);
      var body = animal.GetComponent<Rigidbody>();
      var origin = body.position;
      for (var step = 0; step < 30; step++)
        yield return new WaitForFixedUpdate();

      var displacement = body.position - origin;
      Assert.That(displacement.magnitude, Is.GreaterThan(0.9f));
      Assert.That(Vector3.Dot(displacement.normalized, Vector3.forward), Is.GreaterThan(0.99f));
      Assert.That(Mathf.Abs(Vector3.Dot(body.rotation * Vector3.forward, displacement.normalized)), Is.LessThan(0.01f));
      Assert.That(animal.GetComponent<TastyLabel>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator JumpingPredatorEatsLinearPreyAndShowsItsOwnLabel()
    {
      _root = new GameObject("CompositionTest");
      var camera = _root.AddComponent<Camera>();
      camera.orthographic = true;
      camera.orthographicSize = 9f;
      camera.aspect = 16f / 9f;
      camera.transform.SetPositionAndRotation(Vector3.up * 20f, Quaternion.Euler(90f, 0f, 0f));
      var source = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset");
      _template = Object.Instantiate(source.Prefab);
      Object.DestroyImmediate(_template.GetComponent<LinearMovement>());
      _template.gameObject.AddComponent<JumpMovement>();
      var wander = new SerializedObject(_template.GetComponent<WanderDirection>());
      wander.FindProperty("_minimumInterval").floatValue = 60f;
      wander.FindProperty("_maximumInterval").floatValue = 60f;
      wander.ApplyModifiedPropertiesWithoutUndo();
      _predator = Object.Instantiate(source);
      var predatorSettings = new SerializedObject(_predator);
      predatorSettings.FindProperty("_prefab").objectReferenceValue = _template;
      predatorSettings.ApplyModifiedPropertiesWithoutUndo();
      _prey = Object.Instantiate(source);
      var preySettings = new SerializedObject(_prey);
      preySettings.FindProperty("_role").intValue = (int)AnimalRole.Prey;
      preySettings.ApplyModifiedPropertiesWithoutUndo();
      _predator.Validate();
      _prey.Validate();
      _template.GetComponent<TastyLabel>().ValidateConfiguration();

      var statistics = new DeathStatistics();
      var container = new DiContainer();
      container.Bind<Camera>().FromInstance(camera);
      container.Bind<FoodChain>().FromInstance(new FoodChain(statistics));
      container.Bind<ArenaBounds>().FromInstance(new ArenaBounds(camera));
      container.Bind<IRandomSource>().FromInstance(new MinimumRandom());
      var factory = new AnimalFactory(container, _root.transform);
      var predator = factory.Create(_predator, Vector3.left, Vector3.right);
      var prey = factory.Create(_prey, Vector3.right * 5f, Vector3.left);
      var caption = predator.GetComponentInChildren<Text>(includeInactive: true);
      var label = caption.GetComponentInParent<Canvas>(includeInactive: true).gameObject;
      var preyCaption = prey.GetComponentInChildren<Text>(includeInactive: true);
      var preyLabel = preyCaption.GetComponentInParent<Canvas>(includeInactive: true).gameObject;
      var preyOrigin = prey.GetComponent<Rigidbody>().position;
      var predatorOrigin = predator.GetComponent<Rigidbody>().position;
      var meals = 0;
      var deaths = 0;
      var sawJump = false;
      var sawLinearMovement = false;
      predator.Ate += () => meals++;
      prey.Died += () => deaths++;
      for (var step = 0; step < 250 && deaths == 0; step++)
      {
        yield return new WaitForFixedUpdate();
        sawJump |= predator.GetComponent<JumpMovement>().IsJumping && predator.Visual.localPosition.y > 0.1f;
        if (prey != null && prey.IsAlive)
        {
          sawLinearMovement |= Vector3.Distance(preyOrigin, prey.GetComponent<Rigidbody>().position) > 0.5f;
          Assert.That(preyLabel.activeSelf, Is.False);
        }
      }
      Assert.That(sawJump && sawLinearMovement, Is.True);
      Assert.That(Vector3.Distance(predatorOrigin, predator.GetComponent<Rigidbody>().position), Is.GreaterThan(0.5f));
      Assert.That(predator.IsAlive, Is.True);
      Assert.That(deaths, Is.EqualTo(1));
      Assert.That(meals, Is.EqualTo(1));
      Assert.That(statistics.PreyDeaths, Is.EqualTo(1));
      Assert.That(statistics.PredatorDeaths, Is.Zero);
      Assert.That(caption.text, Is.EqualTo("Tasty!"));
      Assert.That(label.activeSelf, Is.True);
      yield return null;
      Assert.That(prey == null, Is.True);
      yield return new WaitForSeconds(1.1f);
      Assert.That(label.activeSelf, Is.False);
      TestContext.WriteLine("Existing factory created jumping Predator and linear Prey; physical contact: one death, one meal, Tasty!.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
      Object.Destroy(_root);
      if (_template != null)
        Object.Destroy(_template.gameObject);
      Object.Destroy(_predator);
      Object.Destroy(_prey);
      Object.Destroy(_catalog);
      yield return null;
      if (_worldScene.IsValid() && _worldScene.isLoaded)
      {
        SceneManager.SetActiveScene(SceneManager.CreateScene("CompositionTestCleanup"));
        yield return SceneManager.UnloadSceneAsync(_worldScene);
      }
      LogAssert.NoUnexpectedReceived();
    }

    private class MinimumRandom : IRandomSource
    {
      public float Range(float minimum, float maximum) => minimum;

      public int Range(int minimum, int maximum) => minimum;
    }
  }
}
#endif
