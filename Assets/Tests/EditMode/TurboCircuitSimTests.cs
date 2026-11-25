using LogitracDrive.TurboCircuit;
using NUnit.Framework;
using UnityEngine;

namespace LogitracDrive.Tests
{
    /// <summary>Plain C# simulation tests (no scene).</summary>
    public class TurboCircuitSimTests
    {
        const float Dt = 1f / 120f;

        static RaceSim NewRacingSim()
        {
            var race = new RaceSim(new Track(), new TurboHandling());
            while (race.phase == RacePhase.Countdown) race.Step(default, Dt);
            Assert.AreEqual(RacePhase.Racing, race.phase);
            return race;
        }

        [Test]
        public void SteeringBotFinishesLapWithCheckpointsInOrder()
        {
            var race = NewRacingSim();
            var track = race.track;
            string order = "";
            int lastCp = 0;

            for (int s = 0; s < 120 * 90 && race.phase != RacePhase.Finished; s++)
            {
                // Aim at the centre line 10 samples ahead.
                var c = race.car;
                int ti = (c.idx + 10) % track.n;
                float want = Mathf.Atan2(track.y[ti] - c.y, track.x[ti] - c.x);
                float d = Mathf.Repeat(want - c.h + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
                race.Step(new CarInput { left = d < -0.02f, right = d > 0.02f, last = d < 0 ? Side.Left : Side.Right }, Dt);

                if (race.nextCp != lastCp) { order += race.nextCp; lastCp = race.nextCp; }
            }

            Assert.AreEqual(RacePhase.Finished, race.phase, "Bot did not finish the lap");
            Assert.AreEqual("123", order, "Checkpoints were not counted in order");
            Assert.That(race.finishedTime, Is.InRange(8f, 30f), "Lap time is implausible");
            TestContext.WriteLine($"Bot lap time: {race.finishedTime:0.000} s");
            Debug.Log($"[TurboCircuit] Bot lap time: {race.finishedTime:0.000} s");
        }

        [Test]
        public void TeleportingToFinishWithoutCheckpointsDoesNotFinish()
        {
            var race = NewRacingSim();
            var track = race.track;
            var c = race.car;

            // Drop the car just before the start/finish line, skipping every checkpoint.
            int i = track.n - 2;
            c.x = track.x[i];
            c.y = track.y[i];
            c.h = Mathf.Atan2(track.ty[i], track.tx[i]);
            c.vx = c.vy = 0f;
            c.idx = i;

            for (int s = 0; s < 120 * 2; s++) race.Step(default, Dt);

            Assert.That(track.Forward(0, c.idx), Is.InRange(1, 60), "Car should have driven across the line");
            Assert.AreNotEqual(RacePhase.Finished, race.phase, "Lap finished without the checkpoints");
            Assert.AreEqual(0, race.nextCp);
        }

        [Test]
        public void HoldingBothChargesBoostAndReleasingFiresIt()
        {
            var race = NewRacingSim();
            var c = race.car;
            var T = new TurboHandling();

            for (int s = 0; s < 180; s++) race.Step(default, Dt); // 1.5 s up the start straight
            Assert.Greater(c.Speed, T.driftMinSpeed);

            race.Step(new CarInput { left = true, last = Side.Left }, Dt);
            for (int s = 0; s < 72; s++) race.Step(new CarInput { left = true, right = true, last = Side.Left }, Dt); // 0.6 s
            Assert.IsTrue(c.drifting, "Holding both at speed should drift");
            Assert.AreEqual(-1, c.driftDir, "Drift should follow the most recently pressed side");
            Assert.GreaterOrEqual(c.charge, T.minCharge, "Drifting should charge boost");
            float charge = c.charge;

            race.Step(default, Dt); // release both
            Assert.IsFalse(c.drifting);
            Assert.AreEqual(0f, c.charge);
            Assert.That(c.boostTime, Is.EqualTo(T.boostBase + T.boostPerCharge * charge - Dt).Within(1e-4f), "Releasing should fire a boost");
        }
    }
}
