using System;
using UnityEngine;

namespace ZooWorld.Simulation
{
  public sealed class ArenaBounds
  {
    private readonly Camera _camera;
    private readonly Plane _ground = new(Vector3.up, Vector3.zero);
    private Matrix4x4 _projection;
    private Matrix4x4 _cameraToWorld;
    private Plane _left;
    private Plane _right;
    private Plane _bottom;
    private Plane _top;
    private Vector3 _center;
    private bool _hasGeometry;

    public Vector3 Center
    {
      get
      {
        Refresh();
        return _center;
      }
    }

    public ArenaBounds(Camera camera)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      _camera = camera;
    }

    public bool Contains(Vector3 position, float radius, float margin = 0f)
    {
      if (radius < 0f || margin < 0f || !float.IsFinite(radius) || !float.IsFinite(margin))
        throw new ArgumentOutOfRangeException(nameof(radius), "Radius and margin must be finite and nonnegative.");

      Refresh();
      var inset = radius + margin;
      return _left.GetDistanceToPoint(position) >= inset && _right.GetDistanceToPoint(position) >= inset
        && _bottom.GetDistanceToPoint(position) >= inset && _top.GetDistanceToPoint(position) >= inset;
    }

    public Vector3 GetReturnDirection(Vector3 position)
    {
      var direction = Center - position;
      direction.y = 0f;
      return direction.normalized;
    }

    private void Refresh()
    {
      var projection = _camera.projectionMatrix;
      var cameraToWorld = _camera.cameraToWorldMatrix;
      if (_hasGeometry && _projection == projection && _cameraToWorld == cameraToWorld)
        return;

      var bottomLeft = Project(0f, 0f);
      var bottomRight = Project(1f, 0f);
      var topRight = Project(1f, 1f);
      var topLeft = Project(0f, 1f);
      _center = (bottomLeft + bottomRight + topRight + topLeft) * 0.25f;
      _left = CreateEdge(bottomLeft, topLeft);
      _right = CreateEdge(topRight, bottomRight);
      _bottom = CreateEdge(bottomRight, bottomLeft);
      _top = CreateEdge(topLeft, topRight);
      _projection = projection;
      _cameraToWorld = cameraToWorld;
      _hasGeometry = true;
    }

    private Vector3 Project(float x, float y)
    {
      var ray = _camera.ViewportPointToRay(new Vector3(x, y, 0f));
      if (!_ground.Raycast(ray, out var distance))
        throw new InvalidOperationException("The arena camera must see the XZ plane through every viewport corner.");

      return ray.GetPoint(distance);
    }

    private Plane CreateEdge(Vector3 start, Vector3 end)
    {
      var normal = Vector3.Cross(end - start, Vector3.up).normalized;
      if (Vector3.Dot(normal, _center - start) < 0f)
        normal = -normal;

      return new Plane(normal, start);
    }
  }
}
