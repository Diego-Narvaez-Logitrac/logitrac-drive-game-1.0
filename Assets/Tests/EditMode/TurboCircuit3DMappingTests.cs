using LogitracDrive.TurboCircuit;
using NUnit.Framework;
using UnityEngine;

namespace LogitracDrive.Tests
{
    public class TurboCircuit3DMappingTests
    {
        [Test]
        public void CarWorldForwardMatchesSimHeading()
        {
            var go = new GameObject("CarPoseProbe");
            try
            {
                var race = new RaceSim(new Track(), new TurboHandling());
                var car = race.car;
                foreach (float h in new[] { 0f, 0.5f, -1.2f, Mathf.PI / 2f, Mathf.PI, 2.6f, 4.7f, -7f })
                {
                    car.h = h;
                    CarView3D.Place(go.transform, car);
                    var expected = new Vector3(Mathf.Cos(h), 0f, -Mathf.Sin(h)).normalized;
                    // Car models face +X in local space.
                    Assert.That(Vector3.Distance(go.transform.right, expected), Is.LessThan(1e-4f), $"heading {h}");
                    Assert.That(Vector3.Distance(TurboSpace3D.Forward(h), expected), Is.LessThan(1e-4f), $"heading {h}");
                }

                // Position: X = x * 0.01, Z = -y * 0.01, Y up.
                car.x = 400f; car.y = 2300f;
                CarView3D.Place(go.transform, car);
                Assert.That(Vector3.Distance(go.transform.position, new Vector3(4f, 0f, -23f)), Is.LessThan(1e-4f));

                // Moving forward in the sim moves the car along its world forward.
                race.Reset();
                while (race.phase == RacePhase.Countdown) race.Step(default, 1f / 120f);
                for (int i = 0; i < 60; i++) race.Step(default, 1f / 120f);
                var before = TurboSpace3D.ToWorld(car.x, car.y);
                race.Step(default, 1f / 120f);
                var moved = (TurboSpace3D.ToWorld(car.x, car.y) - before).normalized;
                Assert.That(Vector3.Distance(moved, TurboSpace3D.Forward(car.h)), Is.LessThan(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
