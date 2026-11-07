using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals.Movement;
using ZooWorld.Simulation;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
  public class ArenaDirectionTests
  {
    private Camera _camera;
    private ArenaBounds _bounds;
    private GameObject _animal;

    [SetUp]
    public void SetUp()
    {
      _camera = new GameObject("ArenaTestCamera").AddComponent<Camera>();
      _camera.orthographic = true;
      _camera.orthographicSize = 9f;
      _camera.aspect = 16f / 9f;
      _camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));
      _bounds = new ArenaBounds(_camera);
    }

    [Test]
    public void BodyRadiusAndChangedAspectUseActualViewport()
    {
      Assert.That(_bounds.Contains(new Vector3(15.4f, 0f, 0f), 0.5f), Is.True);
      Assert.That(_bounds.Contains(new Vector3(15.6f, 0f, 0f), 0.5f), Is.False);
      Assert.That(_bounds.Contains(new Vector3(0f, 0f, 8.6f), 0.5f), Is.False);

      _camera.aspect = 4f / 3f;
      Assert.That(_bounds.Contains(new Vector3(13f, 0f, 0f), 0.5f), Is.False);
      Assert.That(_bounds.Contains(new Vector3(11.4f, 0f, 0f), 0.5f), Is.True);

      _camera.transform.position += new Vector3(5f, 0f, 3f);
      Assert.That(Vector3.Distance(_bounds.Center, new Vector3(5f, 0f, 3f)), Is.LessThan(0.001f));
      _camera.transform.rotation = Quaternion.Euler(90f, 35f, 0f);
      var outside = _camera.ViewportPointToRay(new Vector3(1.1f, 0.5f)).origin;
      outside.y = 0f;
      Assert.That(_bounds.Contains(outside, 0f), Is.False);
    }

    [Test]
    public void SpawnPositionsKeepTheWholeBodyInsideAfterCameraChanges()
    {
      var random = new UnityRandomSource();
      foreach (var aspect in new[] { 16f / 9f, 4f / 3f })
      {
        _camera.aspect = aspect;
        _camera.transform.rotation = Quaternion.Euler(90f, 35f, 0f);
        for (var index = 0; index < 100; index++)
        {
          var position = _bounds.GetRandomPosition(1.5f, random);
          Assert.That(_bounds.Contains(position, 1.5f - 0.0001f), Is.True);
        }
      }
    }

    [Test]
    public void WanderWaitsThenChoosesNormalizedDeterministicDirection()
    {
      var random = new MinimumRandom();
      var wander = CreateWander(random);
      Assert.That(wander.Tick(Vector3.zero, 0.7f, 1f), Is.EqualTo(Vector3.forward));
      Assert.That(wander.Tick(Vector3.zero, 0.7f, 1.1f), Is.EqualTo(Vector3.right));
      Assert.That(random.Calls, Is.EqualTo(3));
    }

    [Test]
    public void ReturnOverridesExpiredWanderUntilWholeBodyPassesMargin()
    {
      var random = new MinimumRandom();
      var wander = CreateWander(random);
      const float Radius = 0.7f;
      var position = new Vector3(17f, 0f, 0f);
      Assert.That(Vector3.Distance(wander.Tick(position, Radius, 100f), Vector3.left), Is.LessThan(0.0001f));
      Assert.That(wander.IsReturning, Is.True);
      Assert.That(random.Calls, Is.EqualTo(1));

      position = new Vector3(16f - Radius - 0.1f, 0f, 0f);
      Assert.That(Vector3.Distance(wander.Tick(position, Radius, 100f), Vector3.left), Is.LessThan(0.0001f));
      Assert.That(wander.IsReturning, Is.True);
      Assert.That(random.Calls, Is.EqualTo(1));

      position = new Vector3(16f - Radius - 1.1f, 0f, 0f);
      Assert.That(Vector3.Distance(wander.Tick(position, Radius, 100f), Vector3.left), Is.LessThan(0.0001f));
      Assert.That(wander.IsReturning, Is.False);
      Assert.That(random.Calls, Is.EqualTo(2));
      Assert.That(Vector3.Distance(wander.Tick(position, Radius, 0.1f), Vector3.left), Is.LessThan(0.0001f));
    }

    [TearDown]
    public void TearDown()
    {
      if (_animal != null)
        Object.DestroyImmediate(_animal);
      Object.DestroyImmediate(_camera.gameObject);
    }

    private WanderDirection CreateWander(IRandomSource random)
    {
      _animal = new GameObject("WanderTestAnimal");
      _animal.SetActive(false);
      var wander = _animal.AddComponent<WanderDirection>();
      wander.Construct(_bounds, random);
      wander.Initialize(Vector3.forward * 7f);
      return wander;
    }

    private class MinimumRandom : IRandomSource
    {
      public int Calls { get; private set; }

      public float Range(float minimum, float maximum)
      {
        Calls++;
        return minimum;
      }

      public int Range(int minimum, int maximum) => minimum;
    }
  }
}
