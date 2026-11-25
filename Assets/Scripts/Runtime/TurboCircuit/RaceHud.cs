using System;
using UnityEngine;
using UnityEngine.UI;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>Race HUD built with UiKit on the shell's 390×844 reference layout (positions from the prototype).</summary>
    public class RaceHud
    {
        public HoldButton LeftButton { get; private set; }
        public HoldButton RightButton { get; private set; }
        public Button ExitButton { get; private set; }
        public Button RetryButton { get; private set; }
        public Button BackButton { get; private set; }
        public GameObject FinishPanel { get; private set; }

        Text _lapTime, _best, _countdown, _hint, _boostLabel, _finishLap, _finishBest;
        Image[] _dots;
        Image _leftBg, _rightBg, _leftRing, _rightRing;
        RectTransform _boostFill;
        Image _boostFillImg;
        GameObject _newBest;
        Font _body, _display;
        FontStyle _displayStyle;

        static readonly Color OnBorder = Palette.Primary;
        static readonly Color OnBg = TurboGfx.Rgba(36, 123, 254, 0.28f);
        static readonly Color OffBorder = Palette.Border;
        static readonly Color OffBg = TurboGfx.Rgba(40, 41, 43, 0.82f);

        public void Build(Transform parent, Font body, Font display)
        {
            _body = body;
            _display = display != null ? display : body;
            _displayStyle = display != null ? FontStyle.Normal : FontStyle.Bold;

            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GamesApp.ReferenceWidth, GamesApp.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var safe = UiKit.Node("SafeArea", canvasGo.transform);
            UiKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildTop(safe);
            BuildBoostMeter(safe);
            BuildButtons(safe);
            BuildCountdown(safe);
            BuildFinish(safe);
        }

        void BuildTop(RectTransform parent)
        {
            ExitButton = UiKit.Button(parent, "ExitButton", "‹  Exit", _body, 15,
                new Color(23f / 255f, 23f / 255f, 23f / 255f, 0.8f), Palette.TextPrimary, 14);
            UiKit.Place((RectTransform)ExitButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(16, -60), new Vector2(16 + 92, -16));

            var top = UiKit.Node("LapInfo", parent);
            UiKit.Place(top, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-110, -112), new Vector2(110, -14));
            UiKit.VStack(top.gameObject, 0, 0, 0, 0, 2, TextAnchor.UpperCenter);
            UiKit.Label(top, "LapTimeLabel", "LAP TIME", _body, 11, Palette.TextSecondary, FontStyle.Bold, TextAnchor.UpperCenter);
            _lapTime = UiKit.Label(top, "LapTime", "0:00.000", _display, 30, Palette.TextPrimary, _displayStyle, TextAnchor.UpperCenter);
            _best = UiKit.Label(top, "Best", "BEST —", _body, 13, Palette.TextSecondary, FontStyle.Bold, TextAnchor.UpperCenter);

            var dots = UiKit.Node("Checkpoints", top);
            UiKit.HStack(dots.gameObject, 8, TextAnchor.MiddleCenter);
            UiKit.Size(dots.gameObject, height: 14);
            _dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var d = UiKit.Node("Checkpoint" + (i + 1), dots);
                _dots[i] = UiKit.Panel(d, Color.white, 4);
                UiKit.Size(d.gameObject, 8, 8);
            }
        }

        void BuildBoostMeter(RectTransform parent)
        {
            // Prototype: bar at x 70..W-70, top edge 196 px above the bottom, 10 tall; label 8 px above.
            _boostLabel = UiKit.Label(parent, "BoostLabel", "BOOST", _body, 11, Palette.TextSecondary, FontStyle.Bold, TextAnchor.LowerLeft);
            UiKit.Place(_boostLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(70, 196 + 4), new Vector2(-70, 196 + 22));

            var track = UiKit.Node("BoostMeter", parent);
            UiKit.Place(track, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(70, 186), new Vector2(-70, 196));
            UiKit.Panel(track, new Color(1, 1, 1, 0.14f));

            _boostFill = UiKit.Node("Fill", track);
            _boostFillImg = UiKit.Panel(_boostFill, Palette.Primary);
            SetFill(0f);
        }

        void BuildButtons(RectTransform parent)
        {
            var row = UiKit.Node("SteerButtons", parent);
            UiKit.Place(row, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(16, 30), new Vector2(-16, 30 + 124));
            var h = UiKit.HStack(row.gameObject, 14);
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            LeftButton = SteerButton(row, "SteerLeft", "‹", out _leftBg, out _leftRing);
            RightButton = SteerButton(row, "SteerRight", "›", out _rightBg, out _rightRing);
        }

        HoldButton SteerButton(RectTransform parent, string name, string glyph, out Image bg, out Image ring)
        {
            var rt = UiKit.Node(name, parent);
            UiKit.Size(rt.gameObject, flexibleWidth: 1);
            bg = UiKit.Panel(rt, OffBg, 24, raycast: true);
            var ringRt = UiKit.Node("Border", rt);
            UiKit.Stretch(ringRt);
            ring = ringRt.gameObject.AddComponent<Image>();
            ring.sprite = TurboGfx.RoundedRing(24, 2);
            ring.type = Image.Type.Sliced;
            ring.color = OffBorder;
            ring.raycastTarget = false;
            var label = UiKit.Label(rt, "Glyph", glyph, _body, 64, Palette.TextPrimary, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Stretch(label.rectTransform, 0, 0, 0, 8);
            return rt.gameObject.AddComponent<HoldButton>();
        }

        void BuildCountdown(RectTransform parent)
        {
            // Prototype: number baseline at 40% down the screen, hint 40 px below it.
            _countdown = UiKit.Label(parent, "Countdown", "", _display, 72, Palette.TextPrimary, _displayStyle, TextAnchor.MiddleCenter);
            UiKit.Place(_countdown.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.6f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -50), new Vector2(0, 90));
            _hint = UiKit.Label(parent, "Hint", "Hold both buttons to drift. Release to boost.", _body, 14,
                Palette.TextSecondary, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Place(_hint.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.6f), new Vector2(0.5f, 0.5f),
                new Vector2(16, -64), new Vector2(-16, -36));
        }

        void BuildFinish(RectTransform parent)
        {
            var overlay = UiKit.Node("FinishPanel", parent);
            UiKit.Stretch(overlay);
            UiKit.Panel(overlay, new Color(0, 0, 0, 0.72f), raycast: true);
            FinishPanel = overlay.gameObject;

            var card = UiKit.Node("Card", overlay);
            UiKit.Place(card, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24, 0), new Vector2(-24, 0));
            UiKit.Panel(card, Palette.Surface, 28, raycast: true);
            UiKit.VStack(card.gameObject, 24, 24, 28, 28, 10, TextAnchor.UpperCenter);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiKit.Label(card, "Title", "LAP COMPLETE", _body, 13, Palette.TextSecondary, FontStyle.Bold, TextAnchor.UpperCenter);
            _finishLap = UiKit.Label(card, "LapTime", "", _display, 48, Palette.TextPrimary, _displayStyle, TextAnchor.UpperCenter);

            var tagRow = UiKit.Node("NewBestRow", card);
            var tagRowLayout = UiKit.HStack(tagRow.gameObject, 0, TextAnchor.MiddleCenter);
            tagRowLayout.childForceExpandWidth = false;
            var tag = UiKit.Node("NewBest", tagRow);
            UiKit.Panel(tag, TurboGfx.Rgba(36, 123, 254, 0.18f), 12);
            UiKit.HStack(tag.gameObject, 0, TextAnchor.MiddleCenter, 12, 12, 5, 5);
            UiKit.Label(tag, "Label", "New personal best", _body, 13, Palette.Link, FontStyle.Bold).horizontalOverflow = HorizontalWrapMode.Overflow;
            _newBest = tagRow.gameObject;

            _finishBest = UiKit.Label(card, "Best", "", _body, 15, Palette.TextSecondary, align: TextAnchor.UpperCenter);

            RetryButton = UiKit.Button(card, "RetryButton", "RETRY", _display, 18, Palette.Primary, Palette.TextPrimary, 16, _displayStyle);
            UiKit.Size(RetryButton.gameObject, height: 56);

            BackButton = UiKit.Button(card, "BackToTurboCircuitButton", "Back to Turbo Circuit", _body, 16,
                new Color(0, 0, 0, 0), Palette.TextPrimary, 0);
            UiKit.Size(BackButton.gameObject, height: 52);
            var ring = UiKit.Node("Border", BackButton.transform);
            UiKit.Stretch(ring);
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = TurboGfx.RoundedRing(16, 1);
            ringImg.type = Image.Type.Sliced;
            ringImg.color = Palette.Border;
            ringImg.raycastTarget = false;

            FinishPanel.SetActive(false);
        }

        void SetFill(float f)
        {
            UiKit.Place(_boostFill, Vector2.zero, new Vector2(Mathf.Clamp01(f), 1f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            _boostFill.gameObject.SetActive(f > 0f);
        }

        public void Refresh(RaceSim race, float? best, TurboHandling T, bool leftHeld, bool rightHeld)
        {
            var c = race.car;
            float shown = race.phase == RacePhase.Finished ? race.finishedTime : race.time;
            _lapTime.text = RaceSim.FormatTime(shown);
            _best.text = "BEST " + RaceSim.FormatTime(best);
            for (int i = 0; i < 3; i++) _dots[i].color = i < race.nextCp ? Palette.Primary : new Color(1, 1, 1, 0.22f);

            _boostLabel.text = c.boostTime > 0 ? "BOOST!" : (c.drifting ? "DRIFT — CHARGING" : "BOOST");
            float fill = c.boostTime > 0 ? Mathf.Min(1f, c.boostTime / (T.boostBase + T.boostPerCharge)) : c.charge;
            _boostFillImg.color = c.boostTime > 0 ? Palette.Link : Palette.Primary;
            SetFill(fill);

            bool counting = race.phase == RacePhase.Countdown;
            bool go = race.phase == RacePhase.Racing && race.time < 0.6f;
            _countdown.gameObject.SetActive(counting || go);
            _hint.gameObject.SetActive(counting);
            if (counting || go)
            {
                string label = counting ? Mathf.CeilToInt(race.countdown).ToString() : "GO";
                float frac = counting ? race.countdown - Mathf.Floor(race.countdown) : 1f - race.time / 0.6f;
                _countdown.text = label;
                _countdown.fontSize = Mathf.RoundToInt(72 + 40 * frac);
                var col = label == "GO" ? Palette.Primary : Palette.TextPrimary;
                col.a = Mathf.Min(1f, 0.35f + frac);
                _countdown.color = col;
            }

            Style(_leftBg, _leftRing, leftHeld);
            Style(_rightBg, _rightRing, rightHeld);
        }

        static void Style(Image bg, Image ring, bool on)
        {
            bg.color = on ? OnBg : OffBg;
            ring.color = on ? OnBorder : OffBorder;
        }

        public void ShowFinish(float lap, float? best, bool newBest)
        {
            _finishLap.text = RaceSim.FormatTime(lap);
            _finishBest.text = "Best lap " + RaceSim.FormatTime(best);
            _newBest.SetActive(newBest);
            FinishPanel.SetActive(true);
        }

        public void HideFinish() => FinishPanel.SetActive(false);
    }
}
