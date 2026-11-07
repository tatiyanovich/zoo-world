using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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
  public class AnimalConfigurationTests
  {
    [Test]
    public void AnimalRequiresExactlyOneEnabledMovementWithValidSettings()
    {
      var source = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/FrogDefinition.asset");
      var template = Object.Instantiate(source.Prefab);
      try
      {
        Object.DestroyImmediate(template.GetComponent<JumpMovement>());
        Assert.Throws<InvalidOperationException>(template.ValidateConfiguration);
        var linear = template.gameObject.AddComponent<LinearMovement>();
        Assert.DoesNotThrow(template.ValidateConfiguration);
        var jump = template.gameObject.AddComponent<JumpMovement>();
        Assert.Throws<InvalidOperationException>(template.ValidateConfiguration);
        Object.DestroyImmediate(jump);
        linear.enabled = false;
        Assert.Throws<InvalidOperationException>(template.ValidateConfiguration);
        linear.enabled = true;
        var settings = new SerializedObject(linear);
        settings.FindProperty("_speed").floatValue = 0f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        Assert.Throws<InvalidOperationException>(template.ValidateConfiguration);
      }
      finally
      {
        Object.DestroyImmediate(template.gameObject);
      }
    }

    [Test]
    public void InstallerRequiresEnabledTastyLabelForPredatorButAllowsPreyWithoutIt()
    {
      var source = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
        "Assets/ZooWorld/Settings/Animals/SnakeDefinition.asset");
      var template = Object.Instantiate(source.Prefab);
      var definition = Object.Instantiate(source);
      var catalog = ScriptableObject.CreateInstance<AnimalCatalog>();
      var settings = ScriptableObject.CreateInstance<SimulationSettings>();
      var root = new GameObject("ConfigurationTest");
      root.SetActive(false);
      try
      {
        SetReference(definition, "_prefab", template);
        var catalogSettings = new SerializedObject(catalog);
        var definitions = catalogSettings.FindProperty("_definitions");
        definitions.arraySize = 1;
        definitions.GetArrayElementAtIndex(0).objectReferenceValue = definition;
        catalogSettings.ApplyModifiedPropertiesWithoutUndo();
        var camera = root.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9f;
        camera.aspect = 16f / 9f;
        camera.transform.SetPositionAndRotation(Vector3.up * 20f, Quaternion.Euler(90f, 0f, 0f));
        var counters = root.AddComponent<DeathCounterView>();
        var preyText = new GameObject("PreyCounter", typeof(RectTransform), typeof(Text));
        var predatorText = new GameObject("PredatorCounter", typeof(RectTransform), typeof(Text));
        preyText.transform.SetParent(root.transform);
        predatorText.transform.SetParent(root.transform);
        SetReference(counters, "_preyCounter", preyText.GetComponent<Text>());
        SetReference(counters, "_predatorCounter", predatorText.GetComponent<Text>());
        var installer = root.AddComponent<WorldInstaller>();
        SetReference(installer, "_worldCamera", camera);
        SetReference(installer, "_animalsRoot", root.transform);
        SetReference(installer, "_animalCatalog", catalog);
        SetReference(installer, "_simulationSettings", settings);
        SetReference(installer, "_deathCounterView", counters);
        var label = template.GetComponent<TastyLabel>();
        label.enabled = false;
        new DiContainer().Inject(installer);
        Assert.Throws<InvalidOperationException>(installer.InstallBindings);
        label.enabled = true;
        Assert.DoesNotThrow(installer.InstallBindings);
        Object.DestroyImmediate(label);
        new DiContainer().Inject(installer);
        Assert.That(Assert.Throws<InvalidOperationException>(installer.InstallBindings).Message,
          Does.Contain("a predator requires a TastyLabel"));
        var definitionSettings = new SerializedObject(definition);
        definitionSettings.FindProperty("_role").intValue = (int)AnimalRole.Prey;
        definitionSettings.ApplyModifiedPropertiesWithoutUndo();
        Assert.DoesNotThrow(installer.InstallBindings);
      }
      finally
      {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(settings);
        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(template.gameObject);
      }
    }

    private static void SetReference(Object target, string property, Object value)
    {
      var settings = new SerializedObject(target);
      settings.FindProperty(property).objectReferenceValue = value;
      settings.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
