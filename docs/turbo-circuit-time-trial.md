# Turbo Circuit time trial

**Status:** Planned · **Milestone:** 2 · **Unity:** 6.3 LTS (`6000.3.29f1`)

From the Turbo Circuit preview, Play opens a portrait, top-down time trial
with one car, one track, and one lap. Match the browser prototype's handling.
The other three games keep their Coming Soon sheets. Rivals, additional tracks,
sound, haptics, upgrades, backend services, and leaderboards are out of scope.

## Scenes and navigation

- Keep Games first in Build Settings. Add `Assets/Scenes/TurboCircuit.unity` second.
- Turbo Circuit's preview Play button calls `SceneManager.LoadScene("TurboCircuit")`.
- Exit and Back to Turbo Circuit reload Games and reopen the Turbo Circuit preview.
  Pass the preview selection through a small static field.
- Extend the existing scene builder to create the race scene with a camera and
  one race root. Build the remaining objects at runtime, following `GamesApp`.
- Keep shell changes limited to navigation. Use `6000.3.29f1` for batch runs.
  Use a scratch copy if the project is open in the Editor. Commit working
  checkpoints locally and leave them unpushed for review.

## Simulation and controls

Keep simulation coordinates in prototype pixels with positive Y pointing down.
Convert for rendering with `worldPos = (x * 0.01, -y * 0.01)` and
`rotationZ = -headingInDegrees`.

Place the race scripts in `Assets/Scripts/Runtime/TurboCircuit/`.

- `TurboTuning.cs` holds all tuning values in a ScriptableObject asset under `Assets/Data/`.
- `TrackData.cs` holds the control points and dimensions below.
- `Track.cs` builds a closed Catmull–Rom spline with 30 samples per segment,
  resampled at 22-unit spacing. Compute tangents, place checkpoints at 25%, 50%,
  and 75% of the samples, and spawn at index 3. For `NearestOnTrack`, search
  ±14 samples around the last index, then project onto the two neighboring segments.
- `CarSim.cs` holds car state and `Step(input, dt)`. Port the browser's `stepCar`
  line for line using plain C# and `Mathf`. Obtain the prototype source before
  claiming an exact handling match.
- `RaceSim.cs` handles the 3-second countdown, racing, lap timer, and finish.
  Checkpoints must be collected in order within the 12-sample forward window.
  Finish only after all three count and the car reaches the 12-sample window past index 0.
- `RaceController.cs` reads input, updates the views, and advances the sim in
  fixed 1/120-second steps from an accumulator in `Update`, capping each frame's
  contribution at 0.1 seconds. Store the best lap in `PlayerPrefs` under
  `turbo-circuit.best`.
- Provide two bottom touch buttons, each about half the width and 124 px tall,
  with a 30 px safe bottom margin. Use `IPointerDownHandler` and
  `IPointerUpHandler` for simultaneous touches. Support Left and Right arrow
  keys through the Input System and track the most recently pressed direction.

## Handling defaults

| Setting | Value | Setting | Value |
|---|---:|---|---:|
| `maxSpeed` | 540 | `accel` | 360 |
| `steerRate` | 2.6 | `steerFullAt` | 170 |
| `steerMinFactor` | 0.35 | `highSpeedSteerCut` | 0.3 |
| `grip` | 7.0 | `driftGrip` | 1.5 |
| `driftYawRate` | 3.1 | `driftMinSpeed` | 150 |
| `driftDrag` | 0.35 | `slideForCharge` | 60 |
| `chargeRate` | 0.8 | `minCharge` | 0.12 |
| `boostSpeed` | 220 | `boostAccel` | 900 |
| `boostBase` | 0.35 | `boostPerCharge` | 1.15 |
| `grassTopSpeed` | 0.42 | `grassDrag` | 1.4 |
| `grassGrip` | 3.5 | `curbDrag` | 0.12 |
| `wallKeep` | 0.6 | `wallBounce` | 0.25 |

The car auto-accelerates toward top speed. Holding both buttons above
`driftMinSpeed` drifts in the most recently pressed direction. Boost charges
while drifting with sideways speed above `slideForCharge`. Releasing either
or both buttons fires boost when charge reaches `minCharge`. Boost lasts
`boostBase + boostPerCharge × charge` seconds. Grass and curbs bleed speed.
At a wall, push the car back to the wall line and reflect outward velocity.
Apply `wallKeep` on hard hits with outward speed above 60.

## Track

Half-width 70, curb 11, wall 150, sample spacing 22. Clockwise control points
in prototype units:

    (400,2300) (400,1500) (400,820) (470,520) (700,330) (1020,300)
    (1300,430) (1450,720) (1420,1020) (1220,1210) (1000,1330) (930,1560)
    (1040,1760) (1300,1820) (1560,1850) (1720,1920) (1760,2010) (1720,2110)
    (1580,2170) (1300,2190) (1000,2300) (720,2450) (520,2470) (430,2410)

## Views and HUD

- `TrackView` uses stacked closed-loop LineRenderers for cyan wall glow, wall,
  grass, white curb with red dashes, asphalt, and faint center dashes. Add a
  chequered start line.
- `CarView` uses child SpriteRenderers with Unity's built-in square sprite.
  Use a silver body, charcoal hood, two orange stripes, dark cabin, and thin
  cyan side lines. Make the car slightly larger than the prototype.
- Emit rear-wheel trails while sliding, a small smoke effect while drifting,
  and a blue-white flame while boosting.
- Use an orthographic follow camera. Blend 65% heading and 35% velocity
  direction, smoothing rotation at 4.5/s. Place the car 60% down the screen
  and zoom out slightly with speed using the prototype's 0.95 → 0.75 range.
- Build the HUD with `UiKit`. Put Exit at top left and lap time, best time,
  and three checkpoint dots at top center. Put the boost meter above the
  steering buttons with BOOST, DRIFT — CHARGING, and BOOST! states.
- Show 3–2–1–GO and the hint “Hold both buttons to drift. Release to boost.”
  Show lap time at the finish, a New personal best tag when earned, a blue
  (`#247BFE`) Retry button, and Back to Turbo Circuit.

## Verification

Run a clean batch compile and the final test suite using Unity `6000.3.29f1`.
Report the steering bot's lap time.

1. Edit Mode — a steering bot finishes a lap with checkpoints counted in order.
   The prototype reference is approximately 12–14 seconds.
2. Edit Mode — teleporting to the finish without the checkpoints does not finish the lap.
3. Edit Mode — holding both buttons at speed charges boost, and releasing fires it.
4. Play Mode — Games → Turbo Circuit → Play loads the race scene.

In the Editor at 390 × 844, check the countdown, arrow-key steering, drift
charging, release-to-boost, and the finish panel. Confirm Retry restarts the
race and both Exit and Back to Turbo Circuit reopen the correct preview.
