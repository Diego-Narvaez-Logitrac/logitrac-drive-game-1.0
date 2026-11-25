using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>Result of <see cref="Track.NearestOnTrack"/>.</summary>
    public struct TrackHit
    {
        public int idx;
        public float dist;
        public float px, py;
    }

    /// <summary>
    /// Evenly spaced centre line (ported from the prototype's buildTrack/nearestOnTrack).
    /// Prototype units; y points down.
    /// </summary>
    public class Track
    {
        public readonly float[] x, y, tx, ty;
        public readonly int n;
        public readonly float halfWidth, curb, wall;
        public readonly int[] checkpoints;
        public readonly int startIndex = 3;

        public Track() : this(TrackData.Points) { }

        public Track(float[,] P)
        {
            halfWidth = TrackData.HalfWidth;
            curb = TrackData.Curb;
            wall = TrackData.Wall;
            float spacing = TrackData.Spacing;

            // Closed Catmull-Rom, 30 samples per segment.
            int np = P.GetLength(0);
            int rawCount = np * 30;
            var rx = new float[rawCount];
            var ry = new float[rawCount];
            int r = 0;
            for (int i = 0; i < np; i++)
            {
                int i0 = (i - 1 + np) % np, i1 = i, i2 = (i + 1) % np, i3 = (i + 2) % np;
                for (int k = 0; k < 30; k++)
                {
                    float t = k / 30f, t2 = t * t, t3 = t2 * t;
                    rx[r] = CatmullRom(P[i0, 0], P[i1, 0], P[i2, 0], P[i3, 0], t, t2, t3);
                    ry[r] = CatmullRom(P[i0, 1], P[i1, 1], P[i2, 1], P[i3, 1], t, t2, t3);
                    r++;
                }
            }

            // Resample to even spacing so indices map to distance.
            var px = new System.Collections.Generic.List<float> { rx[0] };
            var py = new System.Collections.Generic.List<float> { ry[0] };
            float carry = 0f;
            for (int i = 0; i < rawCount; i++)
            {
                int j = (i + 1) % rawCount;
                float ax = rx[i], ay = ry[i], bx = rx[j], by = ry[j];
                float seg = Hypot(bx - ax, by - ay);
                float d = spacing - carry;
                while (d <= seg)
                {
                    float u = d / seg;
                    px.Add(ax + (bx - ax) * u);
                    py.Add(ay + (by - ay) * u);
                    d += spacing;
                }
                carry = seg - (d - spacing);
            }
            int last = px.Count - 1;
            if (Hypot(px[last] - px[0], py[last] - py[0]) < spacing * 0.5f)
            {
                px.RemoveAt(last);
                py.RemoveAt(last);
            }

            n = px.Count;
            x = px.ToArray();
            y = py.ToArray();
            tx = new float[n];
            ty = new float[n];
            for (int i = 0; i < n; i++)
            {
                int a = (i - 1 + n) % n, b = (i + 1) % n;
                float ang = Mathf.Atan2(y[b] - y[a], x[b] - x[a]);
                tx[i] = Mathf.Cos(ang);
                ty[i] = Mathf.Sin(ang);
            }

            checkpoints = new[] { Mathf.FloorToInt(n * 0.25f), Mathf.FloorToInt(n * 0.5f), Mathf.FloorToInt(n * 0.75f) };
        }

        static float CatmullRom(float p0, float p1, float p2, float p3, float t, float t2, float t3)
        {
            return 0.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
        }

        public static float Hypot(float a, float b) => Mathf.Sqrt(a * a + b * b);

        /// <summary>Samples ahead of <paramref name="a"/> to reach <paramref name="b"/> (0..n-1).</summary>
        public int Forward(int a, int b) => ((b - a) % n + n) % n;

        /// <summary>Nearest sample within ±14 of <paramref name="lastIdx"/>, then the exact point on its two segments.</summary>
        public TrackHit NearestOnTrack(float cx, float cy, int lastIdx)
        {
            int best = lastIdx;
            float bestD = float.PositiveInfinity;
            for (int k = -14; k <= 14; k++)
            {
                int i = ((lastIdx + k) % n + n) % n;
                float dx = cx - x[i], dy = cy - y[i];
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = i; }
            }

            float bx = x[best], by = y[best], cd = Mathf.Sqrt(bestD);
            for (int s = 0; s < 2; s++)
            {
                int j = s == 0 ? (best - 1 + n) % n : best;
                int j2 = (j + 1) % n;
                float ax = x[j], ay = y[j];
                float abx = x[j2] - ax, aby = y[j2] - ay;
                float u = ((cx - ax) * abx + (cy - ay) * aby) / (abx * abx + aby * aby);
                u = Mathf.Max(0f, Mathf.Min(1f, u));
                float qx = ax + abx * u, qy = ay + aby * u, d = Hypot(cx - qx, cy - qy);
                if (d < cd) { cd = d; bx = qx; by = qy; }
            }
            return new TrackHit { idx = best, dist = cd, px = bx, py = by };
        }
    }
}
