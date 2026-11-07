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
using ZooWorld.Simulation;
using ZooWorld.UI;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
  public class AnimalUiTests
  {
    private GameObject _labelTestRoot;

    [UnityTest]
    public IEnumerator CountersRefreshAfterDisablingAndSceneReload()
    {
      const string scenePath = "Assets/ZooWorld/Scenes/World.unity";
      yield return SceneManager.LoadSceneAsync(scenePath);
      var statistics = Object.FindFirstObjectByType<SceneContext>().Container.Resolve<DeathStatistics>();
      var view = Object.FindFirstObjectByType<DeathCounterView>();
      var texts = view.GetComponentsInChildren<Text>();
      Assert.That(texts[0].text, Is.EqualTo("Prey deaths: 0"));
      Assert.That(texts[1].text, Is.EqualTo("Predator deaths: 0"));
      statistics.RecordDeath(AnimalRole.Prey);
      statistics.RecordDeath(AnimalRole.Predator);
      Assert.That(texts[0].text, Is.EqualTo("Prey deaths: 1"));
      Assert.That(texts[1].text, Is.EqualTo("Predator deaths: 1"));
      for (var cycle = 0; cycle < 3; cycle++)
      {
        view.gameObject.SetActive(false);
        statistics.RecordDeath(AnimalRole.Prey);
        Assert.That(texts[0].text, Is.EqualTo($"Prey deaths: {cycle + 1}"));
        view.gameObject.SetActive(true);
        Assert.That(texts[0].text, Is.EqualTo($"Prey deaths: {cycle + 2}"));
      }
      view.enabled = false;
      statistics.RecordDeath(AnimalRole.Predator);
      Assert.That(texts[1].text, Is.EqualTo("Predator deaths: 1"));
      view.enabled = true;
      Assert.That(texts[1].text, Is.EqualTo("Predator deaths: 2"));
      yield return SceneManager.LoadSceneAsync(scenePath);
      texts = Object.FindFirstObjectByType<DeathCounterView>().GetComponentsInChildren<Text>();
      Assert.That(texts[0].text, Is.EqualTo("Prey deaths: 0"));
      Assert.That(texts[1].text, Is.EqualTo("Predator deaths: 0"));
      statistics.RecordDeath(AnimalRole.Prey);
      LogAssert.NoUnexpectedReceived();
      var scene = SceneManager.GetActiveScene();
      SceneManager.SetActiveScene(SceneManager.CreateScene("UiTestCleanup"));
      yield return SceneManager.UnloadSceneAsync(scene);
    }

    [UnityTest]
    public IEnumerator TastyFollowsEaterRefreshesTimerAndHidesOnDisableOrDeath()
    {
      var cameraObject = new GameObject("LabelTestCamera");
      _labelTestRoot = cameraObject;
      var camera = cameraObject.AddComponent<Camera>();
      camera.orthographic = true;
      camera.orthographicSize = 9f;
      camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
      var foodChain = new FoodChain(new DeathStatistics());
      var container = new DiContainer();
      container.Bind<Camera>().FromInstance(camera);
      container.Bind<FoodChain>().FromInstance(foodChain);
      container.Bind<ArenaBounds>().FromInstance(new ArenaBounds(camera));
      container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();
      var factory = new AnimalFactory(container, camera.transform);
      var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset");
      var eater = factory.Create(definition, Vector3.zero, Vector3.forward);
      eater.GetComponent<LinearMovement>().enabled = false;
      var label = eater.GetComponent<TastyLabel>();
      var caption = eater.GetComponentInChildren<Text>(includeInactive: true);
      var labelObject = caption.GetComponentInParent<Canvas>(includeInactive: true).gameObject;
      Assert.That(caption.text, Is.EqualTo("Tasty!"));
      Assert.That(labelObject.activeSelf, Is.False);
      Eat();
      Assert.That(labelObject.activeSelf, Is.True);
      var initialPosition = labelObject.transform.position;
      eater.GetComponent<Rigidbody>().position += Vector3.right * 2f;
      eater.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
      for (var step = 0; step < 10; step++)
      {
        yield return new WaitForFixedUpdate();
        yield return null;
        if (Mathf.Abs(labelObject.transform.position.x - initialPosition.x - 2f) < 0.05f)
          break;
      }
      Assert.That(labelObject.transform.position.x - initialPosition.x, Is.EqualTo(2f).Within(0.05f));
      Assert.That(Quaternion.Angle(labelObject.transform.rotation, camera.transform.rotation), Is.LessThan(0.01f));
      Assert.That(camera.WorldToViewportPoint(labelObject.transform.position).y,
        Is.LessThan(camera.WorldToViewportPoint(eater.transform.position).y));
      yield return new WaitForSeconds(0.65f);
      Eat();
      yield return new WaitForSeconds(0.55f);
      Assert.That(labelObject.activeSelf, Is.True);
      Assert.That(eater.GetComponentsInChildren<TastyLabel>(includeInactive: true), Has.Length.EqualTo(1));
      yield return new WaitForSeconds(0.55f);
      Assert.That(labelObject.activeSelf, Is.False);
      for (var cycle = 0; cycle < 3; cycle++)
      {
        Eat();
        Assert.That(labelObject.activeSelf, Is.True);
        label.enabled = false;
        Assert.That(labelObject.activeSelf, Is.False);
        Eat();
        Assert.That(labelObject.activeSelf, Is.False);
        label.enabled = true;
        Assert.That(labelObject.activeSelf, Is.False);
      }
      Eat();
      eater.gameObject.SetActive(false);
      Assert.That(labelObject.activeSelf, Is.False);
      eater.gameObject.SetActive(true);
      Assert.That(labelObject.activeSelf, Is.False);
      Eat();
      Assert.That(labelObject.activeSelf, Is.True);
      eater.TryDie();
      Assert.That(labelObject.activeSelf, Is.False);
      yield return null;
      LogAssert.NoUnexpectedReceived();

      void Eat()
      {
        var victim = factory.Create(definition, Vector3.right * 10f, Vector3.forward);
        foodChain.ResolveContact(eater, victim);
      }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
      if (_labelTestRoot != null)
        Object.Destroy(_labelTestRoot);
      yield return null;
      LogAssert.NoUnexpectedReceived();
    }
  }
}
#endif
