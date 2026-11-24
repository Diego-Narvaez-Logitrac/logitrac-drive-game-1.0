using System;
using UnityEngine;

namespace LogitracDrive
{
    /// <summary>One game shown on the Games screen and in its preview.</summary>
    [Serializable]
    public class GameEntry
    {
        public string id;
        public string title;
        [Tooltip("One-line description shown on the card.")]
        public string tagline;
        [Tooltip("Short controls/length line shown on the card.")]
        public string cardMeta;
        [TextArea(2, 5)] public string description;
        public string roundLength;
        public string mode;
        public string controls;
        public string[] howToPlay;
        public string comingSoonMessage;
        [Tooltip("File name (no extension) expected in Assets/Art/Thumbnails.")]
        public string artworkFile;
        public Sprite artwork;
    }

    /// <summary>Locked game definitions for the first milestone (copy only, no gameplay).</summary>
    public static class GameCatalog
    {
        public const string ConvoyRush = "convoy-rush";
        public const string HighwayBlitz = "highway-blitz";
        public const string PrecisionGarage = "precision-garage";
        public const string TurboCircuit = "turbo-circuit";

        public static GameEntry[] CreateDefaults()
        {
            return new[]
            {
                new GameEntry
                {
                    id = ConvoyRush,
                    title = "Convoy Rush",
                    tagline = "Collect cars, grow your convoy, and don’t crash into your own tail.",
                    cardMeta = "Drag to steer · 30–90 s rounds",
                    description = "Drive around the arena and pick up cars to grow your convoy. The longer it gets, the harder it is to steer without hitting walls, traffic or your own tail.",
                    roundLength = "30–90 s rounds",
                    mode = "Endless",
                    controls = "Drag to steer",
                    howToPlay = new[]
                    {
                        "Drag anywhere to steer your lead car.",
                        "Drive into glowing cars to add them to your convoy.",
                        "Avoid walls, traffic and your own convoy. Score: cars collected + time survived.",
                    },
                    comingSoonMessage = "Convoy Rush gameplay is in development. This button will start a run.",
                    artworkFile = "Blue Hour Convoy Rush",
                },
                new GameEntry
                {
                    id = HighwayBlitz,
                    title = "Highway Blitz",
                    tagline = "Weave through highway traffic at full speed. Near misses score big.",
                    cardMeta = "Swipe lanes · 45–120 s rounds",
                    description = "Race down a three-lane highway as traffic gets faster and denser. Dodge cars, chain near misses and push your distance.",
                    roundLength = "45–120 s rounds",
                    mode = "Endless",
                    controls = "Swipe lanes",
                    howToPlay = new[]
                    {
                        "Swipe left or right to change lanes.",
                        "Pass close to cars for near-miss bonuses and combos.",
                        "One crash ends the run. Score: distance + near-miss bonus.",
                    },
                    comingSoonMessage = "Highway Blitz gameplay is in development. This button will start a run.",
                    artworkFile = "Logitrac midnight highway blitz-3",
                },
                new GameEntry
                {
                    id = PrecisionGarage,
                    title = "Precision Garage",
                    tagline = "Draw the perfect path and park without touching a single cone.",
                    cardMeta = "Draw a path · 3-star levels",
                    description = "Guide each vehicle into its marked space without touching cones, walls or other cars. Clean, quick parking earns three stars.",
                    roundLength = "20–90 s levels",
                    mode = "Levels · 1–3 stars",
                    controls = "Draw a path",
                    howToPlay = new[]
                    {
                        "Draw a path from your car to the glowing space.",
                        "Lift your finger and the car follows your path.",
                        "No contact and a fast finish earn all three stars.",
                    },
                    comingSoonMessage = "Precision Garage gameplay is in development. This button will start the first level.",
                    artworkFile = "Silver Logitrac Van in Precision Garage-1",
                },
                new GameEntry
                {
                    id = TurboCircuit,
                    title = "Turbo Circuit",
                    tagline = "Race mini cars on tight tracks. Drift the corners to fill your boost.",
                    cardMeta = "Hold to steer · 60–180 s races",
                    description = "Race compact cars around tight tracks against rivals and the clock. Drift through corners to charge your boost.",
                    roundLength = "60–180 s races",
                    mode = "Vs rivals · lap time",
                    controls = "Hold to steer",
                    howToPlay = new[]
                    {
                        "Your car accelerates on its own.",
                        "Hold the left or right side of the screen to steer.",
                        "Hold both sides to drift and fill your boost.",
                    },
                    comingSoonMessage = "Turbo Circuit gameplay is in development. This button will start a race.",
                    artworkFile = "Logitrac GTs Charge Through the Apex-2",
                },
            };
        }
    }
}
