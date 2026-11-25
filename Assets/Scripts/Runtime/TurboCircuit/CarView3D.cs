using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Placeholder 3D car from built-in primitives, laid out like the 2D car (1.4× prototype size).
    /// Model space: +X forward, +Y up, −Z = the car's right (prototype +y). Visual only: steering front wheels,
    /// body roll from sideways slip (max 6°), slight nose-up pitch while boosting, and the 2D effects.
    /// </summary>
    public class CarView3D : MonoBehaviour
    {
        public const float CarScale = 1.4f;
        const float U = CarScale * TurboSpace3D.Scale; // prototype unit → world, at car scale
        const float MaxRoll = 6f, BoostPitch = 2f, MaxWheelSteer = 22f;
        const float WheelRadius = 0.065f, WheelWidth = 0.05f;

        Transform _body, _flame, _wheelFL, _wheelFR;
        TrailRenderer[] _trails;
        ParticleSystem _smoke;
        float _roll, _pitch;
        Material _lit, _unlit;
        readonly Dictionary<Color, Material> _mats = new Dictionary<Color, Material>();
        readonly Vector2[] _rearWheels = { new Vector2(-12, -10), new Vector2(-12, 10) };

        /// <summary>World pose for a car: used by Sync and by the coordinate-mapping test.</summary>
        public static void Place(Transform t, CarSim c)
        {
            t.SetPositionAndRotation(TurboSpace3D.ToWorld(c.x, c.y), TurboSpace3D.Rotation(c.h));
        }

        public void Build(Material lit, Material unlit, Material sprite)
        {
            _lit = lit;
            _unlit = unlit;
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");

            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);

            // (x, y) rect in prototype car space like the 2D drawCar; heights in world units.
            Box("Body", -22, -11, 44, 22, 0.035f, 0.14f, Palette.Hex(0xb9bfc5), cube);
            Box("BodyHighlight", -20, -10, 40, 6, 0.14f, 0.142f, Palette.Hex(0xe3e7ea), cube);
            Box("Hood", 6, -10, 15, 20, 0.14f, 0.152f, Palette.Hex(0x2b2e33), cube);
            Box("Rear", -21, -10, 6, 20, 0.14f, 0.152f, Palette.Hex(0x2b2e33), cube);
            Box("StripeL", -21, -3.6f, 42, 2.4f, 0.152f, 0.156f, Palette.Hex(0xff9a1f), cube);
            Box("StripeR", -21, 1.2f, 42, 2.4f, 0.152f, 0.156f, Palette.Hex(0xff9a1f), cube);
            Box("Cabin", -9, -8, 16, 16, 0.14f, 0.215f, Palette.Hex(0x121418), cube);
            Box("Wing", -24, -12, 3, 24, 0.17f, 0.19f, Palette.Hex(0x1c1e22), cube);
            Box("CyanL", -12, -11.6f, 20, 1.2f, 0.085f, 0.1f, TurboGfx.Rgba(64, 224, 255), cube, emissive: true);
            Box("CyanR", -12, 10.4f, 20, 1.2f, 0.085f, 0.1f, TurboGfx.Rgba(64, 224, 255), cube, emissive: true);
            Box("HeadL", 20, -9, 2.6f, 4, 0.08f, 0.11f, Palette.Hex(0xfffbe6), cube, emissive: true);
            Box("HeadR", 20, 5, 2.6f, 4, 0.08f, 0.11f, Palette.Hex(0xfffbe6), cube, emissive: true);
            Box("TailL", -22.6f, -9, 2, 4, 0.08f, 0.11f, Palette.Hex(0xff2d3d), cube, emissive: true);
            Box("TailR", -22.6f, 5, 2, 4, 0.08f, 0.11f, Palette.Hex(0xff2d3d), cube, emissive: true);

            // Wheels sit on the root (no body roll); front wheels steer.
            Wheel("WheelRL", -10.5f, -12.5f, cylinder);
            Wheel("WheelRR", -10.5f, 12.5f, cylinder);
            _wheelFL = Wheel("WheelFL", 13.5f, -12.5f, cylinder);
            _wheelFR = Wheel("WheelFR", 13.5f, 12.5f, cylinder);

            // Boost flame: the 2D flame sprite laid flat behind the car.
            var flame = new GameObject("BoostFlame");
            flame.transform.SetParent(_body, false);
            flame.transform.localPosition = new Vector3(-21 * U, 0.09f, 0f);
            flame.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var fsr = flame.AddComponent<SpriteRenderer>();
            fsr.sprite = TurboGfx.Flame;
            fsr.sharedMaterial = sprite;
            fsr.shadowCastingMode = ShadowCastingMode.Off;
            _flame = flame.transform;
            flame.SetActive(false);

            _trails = new TrailRenderer[_rearWheels.Length];
            for (int i = 0; i < _rearWheels.Length; i++) _trails[i] = Trail(i, sprite);
            _smoke = Smoke(sprite);
        }

        Material Mat(Color c, bool emissive)
        {
            if (_mats.TryGetValue(c, out var m)) return m;
            m = new Material(emissive ? _unlit : _lit) { name = (emissive ? "Unlit " : "Lit ") + ColorUtility.ToHtmlStringRGB(c), color = c };
            if (!emissive && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.55f);
            _mats[c] = m;
            return m;
        }

        void Box(string name, float x, float y, float w, float h, float z0, float z1, Color color, Mesh cube, bool emissive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_body, false);
            go.transform.localPosition = new Vector3((x + w / 2f) * U, (z0 + z1) / 2f, -(y + h / 2f) * U);
            go.transform.localScale = new Vector3(w * U, z1 - z0, h * U);
            go.AddComponent<MeshFilter>().sharedMesh = cube;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(color, emissive);
            r.shadowCastingMode = emissive ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = false;
        }

        Transform Wheel(string name, float x, float y, Mesh cylinder)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(x * U, WheelRadius, -y * U);
            var go = new GameObject("Tyre");
            go.transform.SetParent(pivot, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // cylinder axis → lateral (Z)
            go.transform.localScale = new Vector3(WheelRadius * 2f, WheelWidth / 2f, WheelRadius * 2f);
            go.AddComponent<MeshFilter>().sharedMesh = cylinder;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(Palette.Hex(0x0d0e10), false);
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = false;
            return pivot;
        }

        TrailRenderer Trail(int i, Material mat)
        {
            var go = new GameObject(i == 0 ? "SkidL" : "SkidR");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(_rearWheels[i].x * U, 0.006f, -_rearWheels[i].y * U);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lay the ribbon flat on the road
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.alignment = LineAlignment.TransformZ;
            tr.time = 1f / 0.12f;
            tr.widthMultiplier = 3f * TurboSpace3D.Scale * CarScale;
            tr.minVertexDistance = 0.03f;
            tr.startColor = TurboGfx.Rgba(10, 10, 12, 0.55f);
            tr.endColor = TurboGfx.Rgba(10, 10, 12, 0f);
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.emitting = false;
            return tr;
        }

        ParticleSystem Smoke(Material mat)
        {
            var go = new GameObject("Smoke");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1f / 1.6f;
            main.startSpeed = 0f;
            main.startSize = 12f * TurboSpace3D.Scale; // same puff size as 2D
            main.startColor = TurboGfx.Rgba(225, 228, 235, 0.28f);
            main.maxParticles = 300;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 22.25f / 6f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(mat) { name = "Smoke3D", mainTexture = TurboGfx.Circle };
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        public void Sync(CarSim c, float steer, float frame, float time)
        {
            Place(transform, c);

            // Body roll toward the slide (outside of the turn), slight nose-up while boosting; eased.
            float k = frame > 0f ? 1f - Mathf.Exp(-10f * frame) : 1f;
            _roll += (Mathf.Clamp(-c.slide / 30f, -MaxRoll, MaxRoll) - _roll) * k;
            _pitch += ((c.boostTime > 0 ? BoostPitch : 0f) - _pitch) * k;
            _body.localRotation = Quaternion.Euler(_roll, 0f, _pitch);

            var wheelYaw = Quaternion.Euler(0f, steer * MaxWheelSteer, 0f);
            _wheelFL.localRotation = wheelYaw;
            _wheelFR.localRotation = wheelYaw;

            bool boosting = c.boostTime > 0;
            if (_flame.gameObject.activeSelf != boosting) _flame.gameObject.SetActive(boosting);
            if (boosting)
            {
                float len = 26f + 14f * Mathf.Sin(time * 40f);
                _flame.localScale = new Vector3((len + 1f) * U, 14f * U, 1f);
            }
        }

        public void StepEffects(CarSim c, bool racing, float dt)
        {
            bool sliding = racing && (Mathf.Abs(c.slide) > 55f || c.drifting);
            foreach (var t in _trails)
                if (t.emitting != sliding) t.emitting = sliding;

            if (racing && c.drifting && Random.value < dt * 30f)
            {
                var w = _rearWheels[Random.value < 0.5f ? 0 : 1];
                float fx = Mathf.Cos(c.h), fy = Mathf.Sin(c.h), rx = -fy, ry = fx;
                var ep = new ParticleSystem.EmitParams
                {
                    position = TurboSpace3D.ToWorld(c.x + (fx * w.x + rx * w.y) * CarScale, c.y + (fy * w.x + ry * w.y) * CarScale, 0.05f),
                    velocity = TurboSpace3D.ToWorldVector(-c.vx * 0.1f, -c.vy * 0.1f),
                    applyShapeToPosition = false,
                };
                _smoke.Emit(ep, 1);
            }
        }

        public void ClearEffects()
        {
            foreach (var t in _trails) { t.emitting = false; t.Clear(); }
            _smoke.Clear(true);
            _roll = _pitch = 0f;
        }
    }
}
