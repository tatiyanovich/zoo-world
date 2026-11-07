using System;
using UnityEngine;

namespace ZooWorld.Animals.Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Animal), typeof(AnimalMotor))]
  public class JumpMovement : MonoBehaviour
  {
    [SerializeField] private float _interval = 1.2f;
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _distance = 2f;
    [SerializeField] private float _height = 0.8f;

    private AnimalMotor _motor;
    private Transform _visual;
    private Vector3 _visualOrigin;
    private Vector3 _visualScale;
    private float _untilNextJump;
    private float _jumpElapsed;

    public float Interval => _interval;
    public float Duration => _duration;
    public float Distance => _distance;
    public bool IsJumping { get; private set; }

    public void ValidateConfiguration()
    {
      if (!enabled || !float.IsFinite(_interval) || !float.IsFinite(_duration)
        || !float.IsFinite(_distance) || !float.IsFinite(_height)
        || _duration <= 0f || _interval < _duration || _distance <= 0f || _height <= 0f)
        throw new InvalidOperationException($"{name}: positive jump settings and interval >= duration are required.");

      GetComponent<AnimalMotor>().ValidateConfiguration();
    }

    private void Awake()
    {
      _motor = GetComponent<AnimalMotor>();
      _visual = GetComponent<Animal>().Visual;
      _visualOrigin = _visual.localPosition;
      _visualScale = _visual.localScale;
    }

    private void OnEnable()
    {
      _untilNextJump = _interval;
      _jumpElapsed = 0f;
      IsJumping = false;
      _visual.localPosition = _visualOrigin;
      _visual.localScale = _visualScale;
    }

    private void FixedUpdate()
    {
      var deltaTime = Time.fixedDeltaTime;
      var direction = _motor.Tick(deltaTime);
      _untilNextJump -= deltaTime;
      if (!IsJumping && _untilNextJump <= 0.00001f)
      {
        _untilNextJump += _interval;
        _jumpElapsed = 0f;
        IsJumping = true;
      }

      if (!IsJumping)
      {
        _motor.Drive(direction, 0f);
        return;
      }

      var stepDuration = Mathf.Min(deltaTime, _duration - _jumpElapsed);
      _motor.Drive(direction, _distance / _duration * stepDuration / deltaTime);
      _jumpElapsed += stepDuration;
      var phase = Mathf.Clamp01(_jumpElapsed / _duration);
      var arc = 4f * phase * (1f - phase);
      _visual.localPosition = _visualOrigin + Vector3.up * (_height * arc);
      _visual.localScale = _visualScale * (1f + 0.2f * arc);
      if (_jumpElapsed >= _duration - 0.00001f)
      {
        IsJumping = false;
        _visual.localPosition = _visualOrigin;
        _visual.localScale = _visualScale;
      }
    }

    private void OnDisable()
    {
      IsJumping = false;
      _untilNextJump = _interval;
      _jumpElapsed = 0f;
      _visual.localPosition = _visualOrigin;
      _visual.localScale = _visualScale;
      _motor.Stop();
    }
  }
}
