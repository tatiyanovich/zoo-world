using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using ZooWorld.Simulation;

namespace ZooWorld.UI
{
  [DisallowMultipleComponent]
  public class DeathCounterView : MonoBehaviour
  {
    [SerializeField] private Text _preyCounter;
    [SerializeField] private Text _predatorCounter;

    private DeathStatistics _statistics;

    [Inject]
    public void Construct(DeathStatistics statistics)
    {
      _statistics = statistics;
    }

    public void ValidateConfiguration()
    {
      if (_preyCounter == null || _predatorCounter == null || _preyCounter == _predatorCounter)
        throw new InvalidOperationException($"{name}: two separate death counter texts are required.");
    }

    private void OnEnable()
    {
      _statistics.Changed += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _statistics.Changed -= Refresh;
    }

    private void Refresh()
    {
      _preyCounter.text = $"Prey deaths: {_statistics.PreyDeaths}";
      _predatorCounter.text = $"Predator deaths: {_statistics.PredatorDeaths}";
    }
  }
}
