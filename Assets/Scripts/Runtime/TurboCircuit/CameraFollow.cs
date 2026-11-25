using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Follow camera ported from the prototype's updateCamera, in prototype units.
    /// The car sits 60% down the screen; the view rotates so the car points up; zoom 0.95 → 0.75 with speed.
    /// </summary>
    public class CameraFollow
    {
        /// <summary>Prototype canvas width; the visible width is ReferenceWidth / zoom prototype units on any screen.</summary>
        public const float ReferenceWidth = 390f;

        public float x, y, a, z;

        public void Reset(CarSim c)
        {
            x = c.x; y = c.y; a = c.h; z = 0.95f;
        }

        public void Tick(CarSim c, float dt, float maxSpeed)
        {
            float sp = c.Speed;
            // Prototype: atan2(v) * 0.35 + h * 0.65, which breaks once the unbounded heading passes ±π.
            // Same blend, done on the wrapped difference so it holds for the whole lap.
            float target = sp > 60f ? c.h + 0.35f * TurboSpace.WrapAngle(Mathf.Atan2(c.vy, c.vx) - c.h) : c.h;
            float d = TurboSpace.WrapAngle(target - a);
            a += d * (1f - Mathf.Exp(-4.5f * dt));
            float k = 1f - Mathf.Exp(-14f * dt);
            x += (c.x - x) * k;
            y += (c.y - y) * k;
            float zt = 0.95f - 0.2f * Mathf.Min(1f, sp / maxSpeed);
            z += (zt - z) * (1f - Mathf.Exp(-2f * dt));
        }

        public void Apply(Camera cam)
        {
            cam.orthographic = true;
            float visibleWidth = ReferenceWidth / z * TurboSpace.Scale;
            cam.orthographicSize = visibleWidth / Mathf.Max(0.01f, cam.aspect) / 2f;

            // Canvas rotate(-a - π/2) points the heading up; same angle in Unity's y-up world.
            cam.transform.rotation = Quaternion.Euler(0f, 0f, (-a - Mathf.PI / 2f) * Mathf.Rad2Deg);
            var focus = TurboSpace.ToWorld(x, y);
            // Focus drawn at 60% down the screen → camera centre is 10% of the view height ahead of it.
            var pos = focus + cam.transform.up * (0.1f * 2f * cam.orthographicSize);
            pos.z = -10f;
            cam.transform.position = pos;
        }
    }
}
