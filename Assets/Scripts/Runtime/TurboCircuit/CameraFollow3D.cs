using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Elevated 3/4 chase camera. Heading blend, rotation rate, follow rate and zoom come from the 2D
    /// <see cref="CameraFollow"/> unchanged; only the projection differs. FOV 40, pitched 55° down,
    /// car about 60% down the screen, pulls back slightly with speed.
    /// </summary>
    public class CameraFollow3D
    {
        public const float FieldOfView = 40f;
        public const float Pitch = 55f;
        /// <summary>Camera-to-car distance at the 2D camera's start zoom (0.95).</summary>
        public const float BaseDistance = 8.5f;
        const float CarHeight = 0.08f;

        readonly CameraFollow _state = new CameraFollow();

        public void Reset(CarSim c) => _state.Reset(c);
        public void Tick(CarSim c, float dt, float maxSpeed) => _state.Tick(c, dt, maxSpeed);

        public void Apply(Camera cam)
        {
            cam.orthographic = false;
            cam.fieldOfView = FieldOfView;

            // Camera +Z must point along the blended heading: yaw = heading + 90°, then pitch down.
            var rot = Quaternion.Euler(Pitch, _state.a * Mathf.Rad2Deg + 90f, 0f);
            float d = BaseDistance * (0.95f / _state.z); // 2D zoom 0.95 → 0.75 becomes a slight pull-back
            var focus = TurboSpace3D.ToWorld(_state.x, _state.y, CarHeight);

            // Put the car at NDC y = -0.2 (60% down the screen): offset the camera along its up axis.
            float below = 0.2f * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * d;
            cam.transform.SetPositionAndRotation(focus - (rot * Vector3.forward) * d + (rot * Vector3.up) * below, rot);
        }
    }
}
