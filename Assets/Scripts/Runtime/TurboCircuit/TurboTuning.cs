using System;
using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Every handling value, in prototype units (1 unit = 1 px, y down). Plain class so the simulation
    /// stays free of Unity types; <see cref="TurboTuning"/> wraps it as an editable asset.
    /// </summary>
    [Serializable]
    public class TurboHandling
    {
        [Tooltip("Top speed on asphalt, px/s (auto-accelerate toward this)")] public float maxSpeed = 500f;
        [Tooltip("Auto-acceleration, px/s^2")] public float accel = 320f;
        [Tooltip("Yaw rate at full steering, rad/s")] public float steerRate = 2.6f;
        [Tooltip("Speed where steering reaches full authority, px/s")] public float steerFullAt = 170f;
        [Tooltip("Steering authority kept at very low speed")] public float steerMinFactor = 0.35f;
        [Tooltip("Fraction of steering lost at top speed")] public float highSpeedSteerCut = 0.2f;
        [Tooltip("How fast sideways slip dies, 1/s")] public float grip = 7.0f;
        [Tooltip("Grip while drifting")] public float driftGrip = 2.2f;
        [Tooltip("Rotation while drifting, rad/s")] public float driftYawRate = 2.4f;
        [Tooltip("Drift only starts above this speed, px/s")] public float driftMinSpeed = 150f;
        [Tooltip("Speed bled per second while drifting (fraction)")] public float driftDrag = 0.45f;
        [Tooltip("Sideways speed needed to charge boost, px/s")] public float slideForCharge = 50f;
        [Tooltip("Boost charge per second of sliding (meter is 0..1)")] public float chargeRate = 0.8f;
        [Tooltip("Smallest charge that fires a boost")] public float minCharge = 0.12f;
        [Tooltip("Extra top speed during boost, px/s")] public float boostSpeed = 220f;
        [Tooltip("Extra acceleration during boost, px/s^2")] public float boostAccel = 900f;
        [Tooltip("Boost duration with a tiny charge, s")] public float boostBase = 0.35f;
        [Tooltip("Extra boost seconds for a full meter, s")] public float boostPerCharge = 1.15f;
        [Tooltip("Top speed multiplier on grass")] public float grassTopSpeed = 0.42f;
        [Tooltip("Extra speed scrub on grass, fraction/s")] public float grassDrag = 1.4f;
        [Tooltip("Grip on grass")] public float grassGrip = 3.5f;
        [Tooltip("Light scrub on the curbs, fraction/s")] public float curbDrag = 0.12f;
        [Tooltip("Speed kept after a hard barrier hit")] public float wallKeep = 0.6f;
        [Tooltip("How much the car bounces off a barrier")] public float wallBounce = 0.25f;
    }

    /// <summary>Tuning asset (Assets/Data/TurboTuning.asset).</summary>
    [CreateAssetMenu(menuName = "Logitrac Drive/Turbo Tuning", fileName = "TurboTuning")]
    public class TurboTuning : ScriptableObject
    {
        public TurboHandling handling = new TurboHandling();
    }
}
