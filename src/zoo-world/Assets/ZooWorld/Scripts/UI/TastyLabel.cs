using System;
using UnityEngine;
using Zenject;
using ZooWorld.Animals;

namespace ZooWorld.UI
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Animal))]
  public class TastyLabel : MonoBehaviour
  {
    [SerializeField] private RectTransform _label;
    [SerializeField] private float _duration = 1f;
    [SerializeField] private float _offset = 1.2f;

    private Animal _animal;
    private Camera _camera;
    private float _remaining;

    [Inject]
    public void Construct(Camera worldCamera)
    {
      _camera = worldCamera;
    }

    public void ValidateConfiguration()
    {
      if (_label == null || _label == transform || !_label.IsChildOf(transform) || _duration <= 0f || _offset < 0f)
        throw new InvalidOperationException($"{name}: a child label, a positive duration and a nonnegative offset are required.");
    }

    private void Awake()
    {
      _animal = GetComponent<Animal>();
    }

    private void OnEnable()
    {
      _animal.Ate += Show;
      Hide();
    }

    private void LateUpdate()
    {
      if (_remaining <= 0f)
        return;

      _remaining -= Time.deltaTime;
      if (_remaining <= 0f)
        Hide();
      else
        RefreshPose();
    }

    private void OnDisable()
    {
      _animal.Ate -= Show;
      Hide();
    }

    private void Show()
    {
      _remaining = _duration;
      RefreshPose();
      _label.gameObject.SetActive(true);
    }

    private void Hide()
    {
      _remaining = 0f;
      _label.gameObject.SetActive(false);
    }

    private void RefreshPose()
    {
      var cameraTransform = _camera.transform;
      _label.SetPositionAndRotation(transform.position + Vector3.up * 0.8f - cameraTransform.up * _offset,
        cameraTransform.rotation);
    }
  }
}
