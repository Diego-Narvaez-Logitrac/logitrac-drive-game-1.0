using UnityEngine;
using UnityEngine.Rendering;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>Stacked closed-loop LineRenderers (prototype render order) plus the chequered start line.</summary>
    public class TrackView : MonoBehaviour
    {
        public void Build(Track t, Material mat)
        {
            var pos = new Vector3[t.n];
            for (int i = 0; i < t.n; i++) pos[i] = TurboSpace.ToWorld(t.x[i], t.y[i]);

            float w2 = t.wall * 2f, hw = t.halfWidth;
            Line("WallGlow", pos, w2 + 16, TurboGfx.Rgba(64, 224, 255, 0.22f), 0, mat);
            Line("Wall", pos, w2 + 9, Palette.Hex(0x45484e), 1, mat);
            Line("Grass", pos, w2, Palette.Hex(0x1e2b23), 2, mat);
            Line("Curb", pos, (hw + t.curb) * 2, Palette.Hex(0xf2f2f2), 3, mat);
            Line("CurbDashes", pos, (hw + t.curb) * 2, Palette.Hex(0xd8333b), 4, mat, 16, 16);
            Line("Asphalt", pos, hw * 2, Palette.Hex(0x2c2e33), 5, mat);
            Line("EdgeLines", pos, hw * 2 - 6, new Color(1, 1, 1, 0.5f), 6, mat);
            Line("AsphaltInner", pos, hw * 2 - 10, Palette.Hex(0x2c2e33), 7, mat);
            Line("CentreDashes", pos, 2.5f, new Color(1, 1, 1, 0.13f), 8, mat, 24, 30);
            StartLine(t, mat, 9);
        }

        void Line(string name, Vector3[] pos, float widthSim, Color color, int order, Material mat, int dashOn = 0, int dashOff = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = pos.Length;
            lr.SetPositions(pos);
            lr.widthMultiplier = widthSim * TurboSpace.Scale;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = order;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.TransformZ;

            if (dashOn > 0)
            {
                // Tile mode repeats the texture once per world unit; scale so one on+off period spans (on+off) prototype units.
                lr.sharedMaterial = new Material(mat) { name = name, mainTexture = TurboGfx.Dash(dashOn, dashOff) };
                lr.textureMode = LineTextureMode.Tile;
                lr.textureScale = new Vector2(1f / ((dashOn + dashOff) * TurboSpace.Scale), 1f);
                lr.numCornerVertices = 0;
            }
            else
            {
                lr.sharedMaterial = mat;
                lr.numCornerVertices = 6; // round joins like the canvas lineJoin='round'
            }
        }

        void StartLine(Track t, Material mat, int order)
        {
            var root = new GameObject("StartLine").transform;
            root.SetParent(transform, false);
            float sx = t.x[0], sy = t.y[0], tx = t.tx[0], ty = t.ty[0];
            float nx = -ty, ny = tx;
            const float sq = 10f;
            var rot = TurboSpace.Rotation(Mathf.Atan2(ty, tx));
            for (int row = 0; row < 2; row++)
            for (int k = -7; k < 7; k++)
            {
                // Square drawn from its corner along the tangent (x) and normal (y), as in the prototype.
                float ox = sx + nx * (k * sq) + tx * (row * sq);
                float oy = sy + ny * (k * sq) + ty * (row * sq);
                float cx = ox + tx * sq / 2 + nx * sq / 2;
                float cy = oy + ty * sq / 2 + ny * sq / 2;
                var go = new GameObject($"Chequer_{row}_{k}");
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(TurboSpace.ToWorld(cx, cy), rot);
                go.transform.localScale = Vector3.one * (sq * TurboSpace.Scale);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = TurboGfx.Square;
                sr.sharedMaterial = mat;
                sr.color = (k + row) % 2 == 0 ? Color.white : Palette.Hex(0x111111);
                sr.sortingOrder = order;
            }
        }
    }
}
