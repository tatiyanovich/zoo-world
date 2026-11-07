#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using ZooWorld.Animals;
using ZooWorld.Animals.Movement;
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
      Object.Destroy(_template.gameObject);
      Object.Destroy(_predator);
      Object.Destroy(_prey);
      yield return null;
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
