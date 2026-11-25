using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    public enum Surface { Asphalt, Curb, Grass }
    public enum Side { Left, Right }

    /// <summary>Steering input for one simulation step.</summary>
    public struct CarInput
    {
        public bool left;
        public bool right;
        /// <summary>The button pressed most recently.</summary>
        public Side last;
    }

    /// <summary>
    /// Car state and physics, ported line for line from the prototype's stepCar.
    /// Prototype units (px, px/s, radians); y points down. No Unity types except Mathf.
    /// </summary>
    public class CarSim
    {
        public float x, y, h, vx, vy, slide;
        public bool drifting;
        public int driftDir = 1;
        public float charge, boostTime, wallHit;
        public Surface surface = Surface.Asphalt;
        public int idx;

        readonly Track _track;
        readonly TurboHandling T;

        public CarSim(Track track, TurboHandling tuning)
        {
            _track = track;
            T = tuning;
            Reset();
        }

        public float Speed => Track.Hypot(vx, vy);

        public void Reset()
        {
            int s = _track.startIndex;
            x = _track.x[s];
            y = _track.y[s];
            h = Mathf.Atan2(_track.ty[s], _track.tx[s]);
            vx = vy = slide = 0f;
            drifting = false;
            driftDir = 1;
            charge = boostTime = wallHit = 0f;
            surface = Surface.Asphalt;
            idx = s;
        }

        void EndDrift()
        {
            drifting = false;
            if (charge >= T.minCharge) boostTime = T.boostBase + T.boostPerCharge * charge;
            charge = 0f;
        }

        public void Step(CarInput input, float dt)
        {
            bool both = input.left && input.right;
            float speed = Track.Hypot(vx, vy);

            // --- drift state: both buttons; direction = most recently pressed ---
            if (both && !drifting && speed > T.driftMinSpeed) { drifting = true; driftDir = input.last == Side.Left ? -1 : 1; }
            if (drifting && (!both || speed < T.driftMinSpeed * 0.5f)) EndDrift();

            // --- steering ---
            float steer = drifting ? driftDir : (input.right ? 1 : 0) - (input.left ? 1 : 0);
            float auth = Mathf.Max(T.steerMinFactor, Mathf.Min(1f, speed / T.steerFullAt)) * (1 - T.highSpeedSteerCut * Mathf.Min(1f, speed / T.maxSpeed));
            float yaw = drifting ? T.driftYawRate * Mathf.Max(T.steerMinFactor, Mathf.Min(1f, speed / T.steerFullAt)) : T.steerRate * auth;
            h += steer * yaw * dt;

            // --- velocity in the car's frame ---
            float fx = Mathf.Cos(h), fy = Mathf.Sin(h), rx = -fy, ry = fx;
            float vF = vx * fx + vy * fy, vL = vx * rx + vy * ry;

            float top = T.maxSpeed, acc = T.accel, grip = drifting ? T.driftGrip : T.grip;
            if (boostTime > 0) { top += T.boostSpeed; acc += T.boostAccel; boostTime = Mathf.Max(0f, boostTime - dt); }
            if (surface == Surface.Grass) { top *= T.grassTopSpeed; grip = Mathf.Min(grip, T.grassGrip); }

            if (vF < top) vF = Mathf.Min(top, vF + acc * dt);
            else vF -= (vF - top) * Mathf.Min(1f, 2.2f * dt);
            if (surface == Surface.Grass) vF -= vF * T.grassDrag * dt;
            if (surface == Surface.Curb) vF -= vF * T.curbDrag * dt;
            if (drifting) vF -= vF * T.driftDrag * dt;
            vL *= Mathf.Exp(-grip * dt);

            if (drifting && Mathf.Abs(vL) > T.slideForCharge) charge = Mathf.Min(1f, charge + T.chargeRate * dt);

            vx = fx * vF + rx * vL; vy = fy * vF + ry * vL;
            x += vx * dt; y += vy * dt;
            slide = vL;

            // --- track surface and barriers ---
            var near = _track.NearestOnTrack(x, y, idx);
            idx = near.idx;
            if (near.dist <= _track.halfWidth) surface = Surface.Asphalt;
            else if (near.dist <= _track.halfWidth + _track.curb) surface = Surface.Curb;
            else surface = Surface.Grass;
            if (near.dist > _track.wall && near.dist > 0)
            {
                float ux = (x - near.px) / near.dist, uy = (y - near.py) / near.dist;
                x = near.px + ux * _track.wall; y = near.py + uy * _track.wall;
                float vn = vx * ux + vy * uy;
                if (vn > 0)
                {
                    vx -= (1 + T.wallBounce) * vn * ux; vy -= (1 + T.wallBounce) * vn * uy;
                    if (vn > 60) { vx *= T.wallKeep; vy *= T.wallKeep; wallHit = 0.25f; }
                }
            }
            if (wallHit > 0) wallHit = Mathf.Max(0f, wallHit - dt);
        }
    }
}
