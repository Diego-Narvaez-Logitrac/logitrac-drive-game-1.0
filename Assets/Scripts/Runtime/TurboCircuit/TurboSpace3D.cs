using UnityEngine;
using UnityEngine.Rendering;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Prototype units (y down) → Unity 3D world (Y up): X = x × 0.01, Z = −y × 0.01.
    /// Models face +X locally, so a yaw of h (radians, as degrees about Y) gives forward = (cos h, 0, −sin h).
    /// </summary>
    public static class TurboSpace3D
    {
        public const float Scale = 0.01f;

        public static Vector3 ToWorld(float x, float y, float height = 0f) => new Vector3(x * Scale, height, -y * Scale);
        public static Vector3 ToWorldVector(float vx, float vy) => new Vector3(vx * Scale, 0f, -vy * Scale);
        public static Quaternion Rotation(float heading) => Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f);
        public static Vector3 Forward(float heading) => new Vector3(Mathf.Cos(heading), 0f, -Mathf.Sin(heading));
    }

    /// <summary>Dusk look shared by the scene builder and runtime: palette, ambient and fog.</summary>
    public static class Race3DLook
    {
        public static readonly Color Ambient = new Color(0.30f, 0.30f, 0.52f);       // cool blue/purple
        public static readonly Color Sky = new Color(0.075f, 0.065f, 0.14f);         // dark sky / fog colour
        public static readonly Color SunColor = new Color(1.0f, 0.74f, 0.52f);       // low warm sun
        public const float SunIntensity = 1.35f;
        public static readonly Vector3 SunEuler = new Vector3(20f, -35f, 0f);        // 20° above the horizon
        public const float FogStart = 14f, FogEnd = 60f;

        /// <summary>Flat ambient with an explicit probe, so no lighting bake is ever needed.</summary>
        public static void ApplyAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(Ambient);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
        }
    }
}
