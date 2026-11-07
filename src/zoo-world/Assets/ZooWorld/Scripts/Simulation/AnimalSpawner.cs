using UnityEngine;
using Zenject;
using ZooWorld.Animals;

namespace ZooWorld.Simulation
{
  public class AnimalSpawner : ITickable
  {
    private readonly AnimalCatalog _catalog;
    private readonly SimulationSettings _settings;
    private readonly IAnimalFactory _factory;
    private readonly ArenaBounds _bounds;
    private readonly IRandomSource _random;
    private float _remainingTime;

    public AnimalSpawner(AnimalCatalog catalog, SimulationSettings settings, IAnimalFactory factory,
      ArenaBounds bounds, IRandomSource random)
    {
      _catalog = catalog;
      _settings = settings;
      _factory = factory;
      _bounds = bounds;
      _random = random;
      _remainingTime = NextInterval();
    }

    public void Tick() => Tick(Time.deltaTime);

    public void Tick(float deltaTime)
    {
      _remainingTime -= deltaTime;
      if (_remainingTime > 0f)
        return;

      var definition = _catalog.Definitions[_random.Range(0, _catalog.Definitions.Count)];
      var position = FindPosition(definition.SpawnRadius);
      var angle = _random.Range(0f, Mathf.PI * 2f);
      var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
      _factory.Create(definition, position, direction);
      _remainingTime = NextInterval();
    }

    private Vector3 FindPosition(float radius)
    {
      var position = _bounds.Center;
      for (var attempt = 0; attempt < _settings.SpawnPositionAttempts; attempt++)
      {
        position = _bounds.GetRandomPosition(radius, _random);
        var occupied = false;
        var colliders = Physics.OverlapSphere(position, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (var collider in colliders)
        {
          if (collider.attachedRigidbody != null)
          {
            occupied = true;
            break;
          }
        }

        if (!occupied)
          return position;
      }

      return position;
    }

    private float NextInterval() => _random.Range(_settings.MinimumSpawnInterval, _settings.MaximumSpawnInterval);
  }
}
