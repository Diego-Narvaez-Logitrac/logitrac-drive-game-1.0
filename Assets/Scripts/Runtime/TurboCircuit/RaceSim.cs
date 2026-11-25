namespace LogitracDrive.TurboCircuit
{
    public enum RacePhase { Countdown, Racing, Finished }

    /// <summary>Countdown → racing → finished, lap timer and in-order checkpoints (ported from stepRace).</summary>
    public class RaceSim
    {
        public const float CountdownSeconds = 3f;
        public const int Window = 12;

        public readonly Track track;
        public readonly CarSim car;

        public RacePhase phase;
        public float countdown;
        public float time;
        public float finishedTime;
        /// <summary>Index of the next checkpoint to cross (0..3; 3 = all done).</summary>
        public int nextCp;

        public RaceSim(Track track, TurboHandling tuning)
        {
            this.track = track;
            car = new CarSim(track, tuning);
            Reset();
        }

        public void Reset()
        {
            car.Reset();
            phase = RacePhase.Countdown;
            countdown = CountdownSeconds;
            time = 0f;
            finishedTime = 0f;
            nextCp = 0;
        }

        /// <summary>Advances one step. Returns true on the step the lap finishes.</summary>
        public bool Step(CarInput input, float dt)
        {
            if (phase == RacePhase.Countdown)
            {
                countdown -= dt;
                if (countdown <= 0) { phase = RacePhase.Racing; time = 0f; }
                return false;
            }
            if (phase != RacePhase.Racing) return false;

            time += dt;
            car.Step(input, dt);
            int idx = car.idx;
            if (nextCp < track.checkpoints.Length)
            {
                if (track.Forward(track.checkpoints[nextCp], idx) <= Window) nextCp++;
            }
            else if (track.Forward(0, idx) <= Window)
            {
                phase = RacePhase.Finished;
                finishedTime = time;
                return true;
            }
            return false;
        }

        public static string FormatTime(float? t)
        {
            if (t == null) return "—";
            float v = t.Value;
            int m = (int)(v / 60f);
            float s = v - m * 60;
            return m + ":" + (s < 10 ? "0" : "") + s.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
