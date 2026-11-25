using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// 3D track generated from the Track samples as a handful of combined meshes (one draw each):
    /// ground, asphalt, red curbs, white curbs, walls, wall top edges, chequer white/black, centre dashes.
    /// Offsets use each sample's normal; on corners tighter than an offset, the inner side of that
    /// offset folds over itself (see <see cref="TightSections"/>).
    /// </summary>
    public class TrackView3D : MonoBehaviour
    {
        public const float WallHeight = 0.4f;
        const float EdgeWidth = 4f;   // prototype units
        const float DashWidth = 3f;   // a little wider than 2D's 2.5 so it survives perspective

        Material _lit, _unlit;
        readonly Dictionary<Color, Material> _mats = new Dictionary<Color, Material>();

        public void Build(Track t, Material lit, Material unlit)
        {
            _lit = lit;
            _unlit = unlit;
            float hw = t.halfWidth, curbOut = t.halfWidth + t.curb, wall = t.wall;

            Ground(t);
            Emit("Asphalt", Lit(Palette.Hex(0x2c2e33), 0.25f), m => Ribbon(m, t, -hw, hw, 0f, _ => true));
            Emit("CurbRed", Lit(Palette.Hex(0xd8333b), 0.3f), m => { Ribbon(m, t, -curbOut, -hw, 0f, i => i % 2 == 0); Ribbon(m, t, hw, curbOut, 0f, i => i % 2 == 0); });
            Emit("CurbWhite", Lit(Palette.Hex(0xf2f2f2), 0.3f), m => { Ribbon(m, t, -curbOut, -hw, 0f, i => i % 2 == 1); Ribbon(m, t, hw, curbOut, 0f, i => i % 2 == 1); });
            Emit("Walls", Lit(Palette.Hex(0x45484e), 0.1f), m => { Wall(m, t, -wall); Wall(m, t, wall); });
            Emit("WallEdges", Unlit(TurboGfx.Rgba(64, 224, 255)), m =>
            {
                Ribbon(m, t, -wall - EdgeWidth / 2, -wall + EdgeWidth / 2, WallHeight + 0.002f, _ => true);
                Ribbon(m, t, wall - EdgeWidth / 2, wall + EdgeWidth / 2, WallHeight + 0.002f, _ => true);
            });
            Emit("ChequerWhite", Lit(Color.white, 0.2f), m => Chequer(m, t, true));
            Emit("ChequerBlack", Lit(Palette.Hex(0x111111), 0.2f), m => Chequer(m, t, false));
            Emit("CentreDashes", Lit(Palette.Hex(0x4a4d53), 0.2f), m => Dashes(m, t));
        }

        // ---------------- materials ----------------

        Material Lit(Color c, float smoothness)
        {
            if (_mats.TryGetValue(c, out var m)) return m;
            m = new Material(_lit) { name = "Lit " + ColorUtility.ToHtmlStringRGB(c), color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off); // folded inner offsets stay visible
            _mats[c] = m;
            return m;
        }

        Material Unlit(Color c)
        {
            var m = new Material(_unlit) { name = "Unlit " + ColorUtility.ToHtmlStringRGB(c), color = c };
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);
            return m;
        }

        // ---------------- mesh assembly ----------------

        class MeshData
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<int> tri = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                // a-b along one edge, c-d the next; winding faces +normal for the up-facing strips.
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(normal); n.Add(normal); n.Add(normal); n.Add(normal);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
                tri.Add(i + 1); tri.Add(i + 2); tri.Add(i + 3);
            }
        }

        void Emit(string name, Material mat, System.Action<MeshData> fill)
        {
            var data = new MeshData();
            fill(data);
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(data.v);
            mesh.SetNormals(data.n);
            mesh.SetTriangles(data.tri, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; // only the car casts
            r.receiveShadows = true;
        }

        static Vector3 Offset(Track t, int i, float d, float h) =>
            TurboSpace3D.ToWorld(t.x[i] + -t.ty[i] * d, t.y[i] + t.tx[i] * d, h); // normal (-ty, tx) = car's right

        static void Ribbon(MeshData m, Track t, float d0, float d1, float h, System.Func<int, bool> include)
        {
            for (int i = 0; i < t.n; i++)
            {
                if (!include(i)) continue;
                int j = (i + 1) % t.n;
                m.Quad(Offset(t, i, d0, h), Offset(t, i, d1, h), Offset(t, j, d0, h), Offset(t, j, d1, h), Vector3.up);
            }
        }

        static void Wall(MeshData m, Track t, float d)
        {
            for (int i = 0; i < t.n; i++)
            {
                int j = (i + 1) % t.n;
                var inward = (Offset(t, i, 0, 0) - Offset(t, i, d, 0)).normalized;
                m.Quad(Offset(t, i, d, 0), Offset(t, i, d, WallHeight), Offset(t, j, d, 0), Offset(t, j, d, WallHeight), inward);
            }
        }

        void Ground(Track t)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < t.n; i++)
            {
                minX = Mathf.Min(minX, t.x[i]); maxX = Mathf.Max(maxX, t.x[i]);
                minY = Mathf.Min(minY, t.y[i]); maxY = Mathf.Max(maxY, t.y[i]);
            }
            const float margin = 2500f; // beyond the fog
            Emit("Ground", Lit(Palette.Hex(0x1e2b23), 0.05f), m => m.Quad(
                TurboSpace3D.ToWorld(minX - margin, minY - margin, -0.02f), TurboSpace3D.ToWorld(minX - margin, maxY + margin, -0.02f),
                TurboSpace3D.ToWorld(maxX + margin, minY - margin, -0.02f), TurboSpace3D.ToWorld(maxX + margin, maxY + margin, -0.02f), Vector3.up));
        }

        static void Chequer(MeshData m, Track t, bool white)
        {
            // Same squares as the 2D start line: 2 rows × 14, 10 units, along the tangent at sample 0.
            float sx = t.x[0], sy = t.y[0], tx = t.tx[0], ty = t.ty[0], nx = -ty, ny = tx;
            const float sq = 10f, h = 0.004f;
            for (int row = 0; row < 2; row++)
            for (int k = -7; k < 7; k++)
            {
                if (((k + row) % 2 == 0) != white) continue;
                float ox = sx + nx * (k * sq) + tx * (row * sq), oy = sy + ny * (k * sq) + ty * (row * sq);
                m.Quad(TurboSpace3D.ToWorld(ox, oy, h), TurboSpace3D.ToWorld(ox + nx * sq, oy + ny * sq, h),
                       TurboSpace3D.ToWorld(ox + tx * sq, oy + ty * sq, h), TurboSpace3D.ToWorld(ox + tx * sq + nx * sq, oy + ty * sq + ny * sq, h), Vector3.up);
            }
        }

        static void Dashes(MeshData m, Track t)
        {
            // 24 on / 30 off along the centre line, like the 2D dashes.
            const float on = 24f, period = 54f, h = 0.003f;
            float travelled = 0f;
            for (int i = 0; i < t.n; i++)
            {
                int j = (i + 1) % t.n;
                float seg = Track.Hypot(t.x[j] - t.x[i], t.y[j] - t.y[i]);
                for (float s = Mathf.Ceil(travelled / period) * period; s < travelled + seg; s += period)
                {
                    float u0 = (s - travelled) / seg, u1 = Mathf.Min(1f, (s + on - travelled) / seg);
                    float ax = Mathf.Lerp(t.x[i], t.x[j], u0), ay = Mathf.Lerp(t.y[i], t.y[j], u0);
                    float bx = Mathf.Lerp(t.x[i], t.x[j], u1), by = Mathf.Lerp(t.y[i], t.y[j], u1);
                    float nx = -t.ty[i] * DashWidth / 2, ny = t.tx[i] * DashWidth / 2;
                    m.Quad(TurboSpace3D.ToWorld(ax - nx, ay - ny, h), TurboSpace3D.ToWorld(ax + nx, ay + ny, h),
                           TurboSpace3D.ToWorld(bx - nx, by - ny, h), TurboSpace3D.ToWorld(bx + nx, by + ny, h), Vector3.up);
                }
                travelled += seg;
            }
        }

        /// <summary>
        /// Corners where the local turn radius is smaller than <paramref name="offset"/>: the inner side of a strip
        /// at that offset overlaps itself there. Returns (startIndex, endIndex, minRadius) runs.
        /// </summary>
        public static List<(int start, int end, float minRadius)> TightSections(Track t, float offset)
        {
            var runs = new List<(int, int, float)>();
            int start = -1; float minR = float.MaxValue;
            for (int i = 0; i <= t.n; i++)
            {
                int a = (i - 1 + t.n) % t.n, b = i % t.n, c = (i + 1) % t.n;
                float da = Mathf.Atan2(t.ty[c], t.tx[c]) - Mathf.Atan2(t.ty[a], t.tx[a]);
                da = Mathf.Abs(TurboSpace.WrapAngle(da));
                float r = da > 1e-5f ? (2f * TrackData.Spacing) / da : float.MaxValue;
                bool tight = r < offset && i < t.n;
                if (tight) { if (start < 0) { start = b; minR = r; } else minR = Mathf.Min(minR, r); }
                else if (start >= 0) { runs.Add((start, (b - 1 + t.n) % t.n, minR)); start = -1; }
            }
            return runs;
        }
    }
}
