namespace LogitracDrive.TurboCircuit
{
    /// <summary>Turbo Circuit track definition, in prototype units (1 unit = 1 px, y points down).</summary>
    public static class TrackData
    {
        public const float HalfWidth = 70f;
        public const float Curb = 11f;
        public const float Wall = 150f;
        public const float Spacing = 22f;

        /// <summary>Closed loop, clockwise.</summary>
        public static readonly float[,] Points =
        {
            { 400, 2300 }, { 400, 1500 }, { 400, 820 }, { 470, 520 }, { 700, 330 }, { 1020, 300 },
            { 1300, 430 }, { 1450, 720 }, { 1420, 1020 }, { 1220, 1210 }, { 1000, 1330 }, { 930, 1560 },
            { 1040, 1760 }, { 1300, 1820 }, { 1560, 1850 }, { 1720, 1920 }, { 1760, 2010 }, { 1720, 2110 },
            { 1580, 2170 }, { 1300, 2190 }, { 1000, 2300 }, { 720, 2450 }, { 520, 2470 }, { 430, 2410 },
        };
    }
}
