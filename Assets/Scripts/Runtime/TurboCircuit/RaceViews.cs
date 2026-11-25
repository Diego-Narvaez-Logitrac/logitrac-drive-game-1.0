using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    public enum RaceViewMode { TwoD, ThreeD }

    /// <summary>Everything visual about the race: track, car, effects and camera. The simulation never sees it.</summary>
    public interface IRaceView
    {
        void Build(Track track);
        void ResetView(CarSim car, Camera cam, float time);
        void StepEffects(CarSim car, bool racing, float dt);
        /// <param name="steer">-1 left, 0, +1 right (drift direction while drifting).</param>
        void Sync(CarSim car, float steer, float frame, float time, float maxSpeed, Camera cam);
    }

    /// <summary>The original top-down view (TrackView, CarView, CameraFollow), unchanged.</summary>
    public class RaceView2D : IRaceView
    {
        readonly Material _mat;
        CarView _car;
        readonly CameraFollow _follow = new CameraFollow();

        public RaceView2D(Material spriteMaterial) { _mat = spriteMaterial; }

        public void Build(Track track)
        {
            new GameObject("Track").AddComponent<TrackView>().Build(track, _mat);
            _car = new GameObject("Car").AddComponent<CarView>();
            _car.Build(_mat);
        }

        public void ResetView(CarSim car, Camera cam, float time)
        {
            _follow.Reset(car);
            _car.ClearEffects();
            _car.Sync(car, time);
            if (cam != null) _follow.Apply(cam);
        }

        public void StepEffects(CarSim car, bool racing, float dt) => _car.StepEffects(car, racing, dt);

        public void Sync(CarSim car, float steer, float frame, float time, float maxSpeed, Camera cam)
        {
            _follow.Tick(car, frame, maxSpeed);
            if (cam != null) _follow.Apply(cam);
            _car.Sync(car, time);
        }
    }

    /// <summary>Perspective 3D view (TrackView3D, CarView3D, CameraFollow3D) of the same race.</summary>
    public class RaceView3D : IRaceView
    {
        readonly Material _lit, _unlit, _sprite;
        CarView3D _car;
        readonly CameraFollow3D _follow = new CameraFollow3D();

        public RaceView3D(Material lit, Material unlit, Material spriteMaterial)
        {
            _lit = lit; _unlit = unlit; _sprite = spriteMaterial;
        }

        public void Build(Track track)
        {
            Race3DLook.ApplyAmbient();
            new GameObject("Track3D").AddComponent<TrackView3D>().Build(track, _lit, _unlit);
            _car = new GameObject("Car3D").AddComponent<CarView3D>();
            _car.Build(_lit, _unlit, _sprite);
        }

        public void ResetView(CarSim car, Camera cam, float time)
        {
            _follow.Reset(car);
            _car.ClearEffects();
            _car.Sync(car, 0f, 0f, time);
            if (cam != null) _follow.Apply(cam);
        }

        public void StepEffects(CarSim car, bool racing, float dt) => _car.StepEffects(car, racing, dt);

        public void Sync(CarSim car, float steer, float frame, float time, float maxSpeed, Camera cam)
        {
            _follow.Tick(car, frame, maxSpeed);
            if (cam != null) _follow.Apply(cam);
            _car.Sync(car, steer, frame, time);
        }
    }
}
