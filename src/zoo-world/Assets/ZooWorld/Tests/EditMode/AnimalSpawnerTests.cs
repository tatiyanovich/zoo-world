using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Simulation;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
  public class AnimalSpawnerTests
  {
    [Test]
    public void TimerChoosesEveryCatalogEntryAndRestartsWithoutCatchUp()
    {
      var catalog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimalCatalog>(
        "Assets/ZooWorld/Settings/Animals/AnimalCatalog.asset"));
      var third = Object.Instantiate(catalog.Definitions[0]);
      var settings = ScriptableObject.CreateInstance<SimulationSettings>();
      var camera = new GameObject("SpawnerTestCamera").AddComponent<Camera>();
      try
      {
        var serialized = new SerializedObject(catalog);
        var definitions = serialized.FindProperty("_definitions");
        definitions.arraySize = 3;
        definitions.GetArrayElementAtIndex(2).objectReferenceValue = third;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        catalog.Validate();
        settings.Validate();
        camera.orthographic = true;
        camera.orthographicSize = 9f;
        camera.aspect = 16f / 9f;
        camera.transform.SetPositionAndRotation(Vector3.up * 20f, Quaternion.Euler(90f, 0f, 0f));
        var bounds = new ArenaBounds(camera);
        var random = new SequenceRandom();
        var factory = new RecordingFactory(bounds);
        var spawner = new AnimalSpawner(catalog, settings, factory, bounds, random);

        spawner.Tick(0.99f);
        Assert.That(factory.Definitions, Is.Empty);
        spawner.Tick(0.02f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(1));
        spawner.Tick(1.99f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(1));
        spawner.Tick(0.02f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(2));
        spawner.Tick(100f);
        Assert.That(factory.Definitions, Is.EqualTo(catalog.Definitions));
        spawner.Tick(0f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(3));
        spawner.Tick(1.99f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(3));
        spawner.Tick(0.02f);
        Assert.That(factory.Definitions.Count, Is.EqualTo(4));
        Assert.That(random.IntervalCalls, Is.EqualTo(5));
      }
      finally
      {
        Object.DestroyImmediate(camera.gameObject);
        Object.DestroyImmediate(settings);
        Object.DestroyImmediate(third);
        Object.DestroyImmediate(catalog);
      }
    }

    private class SequenceRandom : IRandomSource
    {
      private int _definitionIndex;
      public int IntervalCalls { get; private set; }

      public float Range(float minimum, float maximum)
      {
        if (minimum == 1f && maximum == 2f)
          return IntervalCalls++ % 2 == 0 ? minimum : maximum;

        return (minimum + maximum) * 0.5f;
      }

      public int Range(int minimum, int maximum)
      {
        Assert.That(minimum, Is.Zero);
        Assert.That(maximum, Is.EqualTo(3));
        return _definitionIndex++ % maximum;
      }
    }

    private class RecordingFactory : IAnimalFactory
    {
      private readonly ArenaBounds _bounds;
      public List<AnimalDefinition> Definitions { get; } = new();

      public RecordingFactory(ArenaBounds bounds)
      {
        _bounds = bounds;
      }

      public Animal Create(AnimalDefinition definition, Vector3 position, Vector3 direction)
      {
        Assert.That(_bounds.Contains(position, definition.SpawnRadius), Is.True);
        Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        Definitions.Add(definition);
        return null;
      }
    }
  }
}
