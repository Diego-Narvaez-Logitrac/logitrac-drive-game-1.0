using UnityEngine;
using UnityEngine.Rendering;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// The car from child SpriteRenderers on a square sprite (shapes and colours from the prototype's drawCar),
    /// plus the effects: rear-wheel trails while sliding, smoke while drifting, flame while boosting.
    /// </summary>
    public class CarView : MonoBehaviour
    {
        /// <summary>Prototype draws the car at 1.25×; the plan asks for slightly larger.</summary>
        public const float CarScale = 1.4f;

        Transform _flame;
        TrailRenderer[] _trails;
        ParticleSystem _smoke;
        readonly Vector2[] _rearWheels = { new Vector2(-12, -10), new Vector2(-12, 10) };

        public void Build(Material mat)
        {
            // Rects are the prototype's fillRect(x, y, w, h) in car space (x forward, y to the car's right).
            Part("Shadow", -21, -10, 46, 24, new Color(0, 0, 0, 0.45f), 20, mat);
            var tyre = Palette.Hex(0x0d0e10);
            Part("TyreRL", -15, -13, 9, 4, tyre, 21, mat);
            Part("TyreRR", -15, 9, 9, 4, tyre, 21, mat);
            Part("TyreFL", 9, -13, 9, 4, tyre, 21, mat);
            Part("TyreFR", 9, 9, 9, 4, tyre, 21, mat);
            Part("Body", -22, -11, 44, 22, Palette.Hex(0xb9bfc5), 22, mat);
            Part("BodyHighlight", -20, -10, 40, 6, new Color(0.89f, 0.906f, 0.918f, 0.7f), 22, mat);
            Part("Hood", 6, -10, 15, 20, Palette.Hex(0x2b2e33), 23, mat);
            Part("Rear", -21, -10, 6, 20, Palette.Hex(0x2b2e33), 23, mat);
            var orange = Palette.Hex(0xff9a1f);
            Part("StripeL", -21, -3.6f, 42, 2.4f, orange, 24, mat);
            Part("StripeR", -21, 1.2f, 42, 2.4f, orange, 24, mat);
            Part("Cabin", -9, -8, 16, 16, Palette.Hex(0x121418), 25, mat);
            Part("Glass", 1, -7, 5, 14, TurboGfx.Rgba(143, 195, 255, 0.18f), 26, mat);
            Part("Wing", -24, -12, 3, 24, Palette.Hex(0x1c1e22), 27, mat);
            var cyan = TurboGfx.Rgba(64, 224, 255, 0.85f);
            Part("CyanL", -12, -11.6f, 20, 1.2f, cyan, 28, mat);
            Part("CyanR", -12, 10.4f, 20, 1.2f, cyan, 28, mat);
            var head = Palette.Hex(0xfffbe6);
            Part("HeadL", 20, -9, 2.5f, 4, head, 29, mat);
            Part("HeadR", 20, 5, 2.5f, 4, head, 29, mat);
            var tail = Palette.Hex(0xff2d3d);
            Part("TailL", -22.5f, -9, 2, 4, tail, 29, mat);
            Part("TailR", -22.5f, 5, 2, 4, tail, 29, mat);

            // Boost flame behind the car (drawn under the body, like the prototype).
            var flame = new GameObject("BoostFlame");
            flame.transform.SetParent(transform, false);
            flame.transform.localPosition = Local(-21, 0);
            var fsr = flame.AddComponent<SpriteRenderer>();
            fsr.sprite = TurboGfx.Flame;
            fsr.sharedMaterial = mat;
            fsr.sortingOrder = 19;
            _flame = flame.transform;
            flame.SetActive(false);

            _trails = new TrailRenderer[_rearWheels.Length];
            for (int i = 0; i < _rearWheels.Length; i++) _trails[i] = Trail(i, mat);
            _smoke = Smoke(mat);
        }

        static Vector3 Local(float x, float y) => new Vector3(x * CarScale * TurboSpace.Scale, -y * CarScale * TurboSpace.Scale, 0f);

        void Part(string name, float x, float y, float w, float h, Color color, int order, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Local(x + w / 2f, y + h / 2f);
            go.transform.localScale = new Vector3(w * CarScale * TurboSpace.Scale, h * CarScale * TurboSpace.Scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TurboGfx.Square;
            sr.sharedMaterial = mat;
            sr.color = color;
            sr.sortingOrder = order;
        }

        TrailRenderer Trail(int i, Material mat)
        {
            var go = new GameObject(i == 0 ? "SkidL" : "SkidR");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Local(_rearWheels[i].x, _rearWheels[i].y);
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = 1f / 0.12f; // prototype skid alpha fades at 0.12/s
            tr.widthMultiplier = 3f * TurboSpace.Scale;
            tr.minVertexDistance = 0.02f;
            tr.startColor = TurboGfx.Rgba(10, 10, 12, 0.55f);
            tr.endColor = TurboGfx.Rgba(10, 10, 12, 0f);
            tr.numCapVertices = 2;
            tr.sortingOrder = 15;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.alignment = LineAlignment.TransformZ;
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
            main.startLifetime = 1f / 1.6f;             // prototype: life -= dt * 1.6
            main.startSpeed = 0f;
            main.startSize = 12f * TurboSpace.Scale;    // radius 6
            main.startColor = TurboGfx.Rgba(225, 228, 235, 0.28f);
            main.maxParticles = 300;

            var emission = ps.emission;
            emission.enabled = false;                   // emitted manually from the simulation step
            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLife = ps.sizeOverLifetime;     // radius grows 26/s: 6 → 22.25 over its life
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 22.25f / 6f));

            var col = ps.colorOverLifetime;             // alpha = 0.28 * life
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(mat) { name = "Smoke", mainTexture = TurboGfx.Circle };
            r.sortingOrder = 16;
            r.shadowCastingMode = ShadowCastingMode.Off;

            ps.Play();
            return ps;
        }

        /// <summary>Places the car and drives the boost flame.</summary>
        public void Sync(CarSim c, float time)
        {
            transform.SetPositionAndRotation(TurboSpace.ToWorld(c.x, c.y), TurboSpace.Rotation(c.h));
            bool boosting = c.boostTime > 0;
            if (_flame.gameObject.activeSelf != boosting) _flame.gameObject.SetActive(boosting);
            if (boosting)
            {
                float len = 26f + 14f * Mathf.Sin(time * 40f);
                _flame.localScale = new Vector3((len + 1f) * CarScale * TurboSpace.Scale, 14f * CarScale * TurboSpace.Scale, 1f);
            }
        }

        /// <summary>Called every simulation step while racing (prototype stepEffects).</summary>
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
                    position = TurboSpace.ToWorld(c.x + fx * w.x + rx * w.y, c.y + fy * w.x + ry * w.y),
                    velocity = TurboSpace.ToWorldVector(-c.vx * 0.1f, -c.vy * 0.1f),
                    applyShapeToPosition = false,
                };
                _smoke.Emit(ep, 1);
            }
        }

        public void ClearEffects()
        {
            foreach (var t in _trails) { t.emitting = false; t.Clear(); }
            _smoke.Clear(true);
        }
    }
}
