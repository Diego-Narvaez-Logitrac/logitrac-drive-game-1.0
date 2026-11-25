using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogitracDrive
{
    /// <summary>
    /// Milestone 1: portrait Games screen with four cards, one reusable Preview panel and a Coming Soon sheet.
    /// The UI is built from code at startup against a 390x844 reference layout. No gameplay, login or settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class GamesApp : MonoBehaviour
    {
        public const float ReferenceWidth = 390f;
        public const float ReferenceHeight = 844f;

        [SerializeField] GameEntry[] games = GameCatalog.CreateDefaults();
        [Tooltip("Body font (Inter). Leave empty to use Unity's built-in fallback.")]
        [SerializeField] Font bodyFont;
        [Tooltip("Display font (Russo One). Leave empty to use Unity's built-in fallback in bold.")]
        [SerializeField] Font displayFont;

        readonly Dictionary<string, Button> _cardButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, Image> _cardArt = new Dictionary<string, Image>();

        Font _fallbackFont;
        Text _previewDescription, _previewLength, _previewMode, _previewControls, _comingSoonMessage, _previewArtMissing;
        RectTransform _stepsContainer;
        ScrollRect _previewScroll;

        // ---- Public surface (used by navigation and Play Mode tests) ----
        public IReadOnlyList<GameEntry> Games => games;
        public GameEntry CurrentGame { get; private set; }
        public GameObject GamesPanel { get; private set; }
        public GameObject PreviewPanel { get; private set; }
        public GameObject ComingSoonPanel { get; private set; }
        public Image PreviewArt { get; private set; }
        public Text PreviewTitle { get; private set; }
        public Button BackButton { get; private set; }
        public Button PreviewPlayButton { get; private set; }
        public Button ComingSoonCloseButton { get; private set; }
        public Button ComingSoonBackButton { get; private set; }

        public Button GetCardButton(string id) => _cardButtons.TryGetValue(id, out var b) ? b : null;
        public Image GetCardArt(string id) => _cardArt.TryGetValue(id, out var i) ? i : null;

        /// <summary>Called by the editor scene builder to assign data, sprites and fonts.</summary>
        public void Configure(GameEntry[] entries, Font body, Font display)
        {
            games = entries;
            bodyFont = body;
            displayFont = display;
        }

        Font Body => bodyFont != null ? bodyFont : Fallback;
        Font Display => displayFont != null ? displayFont : Fallback;
        FontStyle DisplayStyle => displayFont != null ? FontStyle.Normal : FontStyle.Bold;

        Font Fallback
        {
            get
            {
                if (_fallbackFont == null) _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _fallbackFont;
            }
        }

        void Awake()
        {
            if (games == null || games.Length == 0) games = GameCatalog.CreateDefaults();
            EnsureEventSystem();
            Build();
            ShowGames();

            // Returning from a game scene (e.g. Turbo Circuit Exit/Back) reopens that game's preview.
            if (!string.IsNullOrEmpty(PendingPreviewId))
            {
                var id = PendingPreviewId;
                PendingPreviewId = null;
                ShowPreview(id);
            }
        }

        /// <summary>Preview to open when the Games scene next loads. Set by game scenes before loading Games.</summary>
        public static string PendingPreviewId;

        // Android back button (reported as Escape): close Coming Soon, or return from a preview to Games.
        void Update()
        {
#if LOGITRAC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool back = kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            bool back = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (back) HandleBack();
        }

        /// <summary>Back navigation: closes Coming Soon, else returns from a preview to Games. Returns false at the root.</summary>
        public bool HandleBack()
        {
            if (ComingSoonPanel.activeSelf) { HideComingSoon(); return true; }
            if (PreviewPanel.activeSelf) { ShowGames(); return true; }
            return false;
        }

        // ---------------- Navigation ----------------

        public void ShowGames()
        {
            CurrentGame = null;
            ComingSoonPanel.SetActive(false);
            PreviewPanel.SetActive(false);
            GamesPanel.SetActive(true);
        }

        public void ShowPreview(string id)
        {
            var game = Find(id);
            if (game == null)
            {
                Debug.LogWarning($"[LogitracDrive] Unknown game id '{id}'.");
                return;
            }

            CurrentGame = game;
            PopulatePreview(game);
            ComingSoonPanel.SetActive(false);
            GamesPanel.SetActive(false);
            PreviewPanel.SetActive(true);
            _previewScroll.verticalNormalizedPosition = 1f;
        }

        public void ShowComingSoon()
        {
            if (CurrentGame == null) return;
            if (CurrentGame.id == GameCatalog.TurboCircuit)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("TurboCircuit");
                return;
            }
            _comingSoonMessage.text = CurrentGame.comingSoonMessage;
            ComingSoonPanel.SetActive(true);
        }

        public void HideComingSoon() => ComingSoonPanel.SetActive(false);

        GameEntry Find(string id)
        {
            foreach (var g in games)
                if (g != null && g.id == id) return g;
            return null;
        }

        // ---------------- Build ----------------

        internal static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if LOGITRAC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void Build()
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // portrait: scale by width, extra height becomes scroll room

            var root = (RectTransform)canvasGo.transform;
            var background = UiKit.Node("Background", root);
            UiKit.Stretch(background);
            UiKit.Panel(background, Palette.Background);

            var safe = UiKit.Node("SafeArea", root);
            UiKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildGamesPanel(safe);
            BuildPreviewPanel(safe);
        }

        void BuildGamesPanel(RectTransform parent)
        {
            var panel = UiKit.Node("GamesPanel", parent);
            UiKit.Stretch(panel);
            GamesPanel = panel.gameObject;

            var header = UiKit.Node("Header", panel);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 72);
            var brand = UiKit.Label(header, "Brand", "LOGITRAC <color=#247BFE>DRIVE</color>", Display, 24,
                Palette.TextPrimary, DisplayStyle, TextAnchor.MiddleLeft);
            UiKit.Stretch(brand.rectTransform, 20, 16, 20, 8);

            var content = BuildScroll(panel, "GamesScroll", 72, 0, out _);
            UiKit.VStack(content.gameObject, 20, 20, 4, 24, 20);

            var intro = UiKit.Node("Intro", content);
            UiKit.VStack(intro.gameObject, 0, 0, 0, 0, 4);
            UiKit.Label(intro, "Heading", "Choose your game", Display, 28, Palette.TextPrimary, DisplayStyle);
            UiKit.Label(intro, "Subheading", "Four arcade games. Short rounds. Beat your best.", Body, 14, Palette.TextSecondary);

            foreach (var g in games)
                if (g != null) BuildCard(content, g);
        }

        void BuildCard(RectTransform parent, GameEntry g)
        {
            var card = UiKit.Node("Card_" + g.id, parent);
            UiKit.Panel(card, Palette.Surface, 22);
            card.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UiKit.VStack(card.gameObject, 0, 0, 0, 0, 0);

            // Same 190px-high, center "cover" crop for every card.
            var frame = UiKit.Node("ArtFrame", card);
            UiKit.Size(frame.gameObject, height: 190);
            frame.gameObject.AddComponent<RectMask2D>();
            var art = UiKit.CoverImage(frame, "Artwork");
            UiKit.SetCoverSprite(art, g.artwork);
            if (g.artwork == null) MissingArtLabel(frame, g);
            Badge(frame, "CONCEPT ART", new Vector2(0, 1), new Vector2(12, -12));

            var body = UiKit.Node("Body", card);
            UiKit.VStack(body.gameObject, 16, 16, 16, 16, 10);
            UiKit.Label(body, "Title", g.title, Display, 22, Palette.TextPrimary, DisplayStyle);
            UiKit.Label(body, "Tagline", g.tagline, Body, 14, Palette.TextSecondary, lineSpacing: 1.2f);

            var row = UiKit.Node("Row", body);
            UiKit.HStack(row.gameObject, 12);
            var meta = UiKit.Label(row, "Meta", g.cardMeta, Body, 13, Palette.TextSecondary, align: TextAnchor.MiddleLeft);
            UiKit.Size(meta.gameObject, flexibleWidth: 1);
            var play = UiKit.Button(row, "PlayButton", "Play", Body, 15, Palette.Primary, Palette.TextPrimary, 14);
            UiKit.Size(play.gameObject, 88, 44);

            string id = g.id;
            play.onClick.AddListener(() => ShowPreview(id));
            _cardButtons[id] = play;
            _cardArt[id] = art;
        }

        void BuildPreviewPanel(RectTransform parent)
        {
            var panel = UiKit.Node("PreviewPanel", parent);
            UiKit.Stretch(panel);
            PreviewPanel = panel.gameObject;

            // Hero artwork
            var hero = UiKit.Node("Hero", panel);
            hero.anchorMin = new Vector2(0, 1);
            hero.anchorMax = new Vector2(1, 1);
            hero.pivot = new Vector2(0.5f, 1);
            hero.sizeDelta = new Vector2(0, 310);
            hero.gameObject.AddComponent<RectMask2D>();
            PreviewArt = UiKit.CoverImage(hero, "Artwork");
            _previewArtMissing = UiKit.Label(hero, "MissingArtwork", "", Body, 14, Palette.TextSecondary,
                align: TextAnchor.MiddleCenter);
            UiKit.Stretch(_previewArtMissing.rectTransform, 24, 24, 24, 24);

            var fade = UiKit.Node("Fade", hero);
            fade.anchorMin = new Vector2(0, 0);
            fade.anchorMax = new Vector2(1, 0);
            fade.pivot = new Vector2(0.5f, 0);
            fade.sizeDelta = new Vector2(0, 110);
            var fadeImg = UiKit.Panel(fade, Palette.Background);
            fadeImg.sprite = UiKit.BottomFade();
            Badge(hero, "CONCEPT ART · NOT GAMEPLAY", new Vector2(0, 0), new Vector2(24, 30));

            // Back
            BackButton = UiKit.Button(panel, "BackButton", "‹  Games", Body, 15,
                new Color(23f / 255f, 23f / 255f, 23f / 255f, 0.8f), Palette.TextPrimary, 14);
            var backRt = (RectTransform)BackButton.transform;
            backRt.anchorMin = backRt.anchorMax = new Vector2(0, 1);
            backRt.pivot = new Vector2(0, 1);
            backRt.anchoredPosition = new Vector2(16, -16);
            backRt.sizeDelta = new Vector2(112, 44);
            BackButton.onClick.AddListener(ShowGames);

            // Details
            var content = BuildScroll(panel, "PreviewScroll", 310, 104, out _previewScroll);
            UiKit.VStack(content.gameObject, 24, 24, 4, 20, 18);
            PreviewTitle = UiKit.Label(content, "Title", "", Display, 34, Palette.TextPrimary, DisplayStyle);
            _previewDescription = UiKit.Label(content, "Description", "", Body, 15, Palette.TextSecondary, lineSpacing: 1.25f);

            var facts = UiKit.Node("Facts", content);
            var factsRow = UiKit.HStack(facts.gameObject, 8, TextAnchor.UpperLeft);
            factsRow.childForceExpandWidth = true;
            factsRow.childForceExpandHeight = true;
            _previewLength = FactTile(facts, "RoundLength", "ROUND");
            _previewMode = FactTile(facts, "Mode", "MODE");
            _previewControls = FactTile(facts, "Controls", "CONTROLS");

            UiKit.Label(content, "HowToPlayHeading", "HOW TO PLAY", Body, 13, Palette.TextSecondary, FontStyle.Bold);
            _stepsContainer = UiKit.Node("Steps", content);
            UiKit.VStack(_stepsContainer.gameObject, 0, 0, 0, 0, 12);

            // Play
            var bar = UiKit.Node("PlayBar", panel);
            bar.anchorMin = new Vector2(0, 0);
            bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.sizeDelta = new Vector2(0, 104);
            PreviewPlayButton = UiKit.Button(bar, "PlayButton", "PLAY", Display, 20, Palette.Primary, Palette.TextPrimary, 18, DisplayStyle);
            UiKit.Stretch((RectTransform)PreviewPlayButton.transform, 24, 12, 24, 34);
            PreviewPlayButton.onClick.AddListener(ShowComingSoon);

            BuildComingSoon(panel);
        }

        void BuildComingSoon(RectTransform parent)
        {
            var overlay = UiKit.Node("ComingSoonPanel", parent);
            UiKit.Stretch(overlay);
            UiKit.Panel(overlay, Palette.Scrim, raycast: true); // blocks taps behind the sheet
            ComingSoonPanel = overlay.gameObject;

            // Bottom-docked, exactly the screen width (zero horizontal offsets), shifted down 28 so the bottom
            // rounded corners sit off-screen. Height comes from the ContentSizeFitter.
            const int sheetPaddingX = 24;
            var sheet = UiKit.Node("Sheet", overlay);
            UiKit.Place(sheet, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, -28), new Vector2(0, -28));
            UiKit.Panel(sheet, Palette.Surface, 28, raycast: true);
            UiKit.VStack(sheet.gameObject, sheetPaddingX, sheetPaddingX, 28, 38 + 28, 14, TextAnchor.UpperCenter);
            sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiKit.Label(sheet, "Title", "Coming soon", Display, 24, Palette.TextPrimary, DisplayStyle, TextAnchor.UpperCenter);
            // The layout group sets the message width to the sheet width minus padding; wrap inside it.
            _comingSoonMessage = UiKit.Label(sheet, "Message", "", Body, 15, Palette.TextSecondary,
                align: TextAnchor.UpperCenter, lineSpacing: 1.25f);
            _comingSoonMessage.horizontalOverflow = HorizontalWrapMode.Wrap;
            _comingSoonMessage.verticalOverflow = VerticalWrapMode.Overflow;

            ComingSoonCloseButton = UiKit.Button(sheet, "GotItButton", "Got it", Body, 16, Palette.Primary, Palette.TextPrimary, 16);
            UiKit.Size(ComingSoonCloseButton.gameObject, height: 52);
            ComingSoonCloseButton.onClick.AddListener(HideComingSoon);

            ComingSoonBackButton = UiKit.Button(sheet, "BackToGamesButton", "Back to games", Body, 15,
                new Color(0, 0, 0, 0), Palette.Link, 0);
            UiKit.Size(ComingSoonBackButton.gameObject, height: 44);
            ComingSoonBackButton.onClick.AddListener(ShowGames);
        }

        void PopulatePreview(GameEntry g)
        {
            UiKit.SetCoverSprite(PreviewArt, g.artwork);
            _previewArtMissing.text = g.artwork == null ? $"Artwork missing\n{g.artworkFile}.png" : "";
            PreviewTitle.text = g.title;
            _previewDescription.text = g.description;
            _previewLength.text = g.roundLength;
            _previewMode.text = g.mode;
            _previewControls.text = g.controls;

            for (int i = _stepsContainer.childCount - 1; i >= 0; i--)
            {
                var child = _stepsContainer.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            var steps = g.howToPlay ?? new string[0];
            for (int i = 0; i < steps.Length; i++)
            {
                var row = UiKit.Node("Step" + (i + 1), _stepsContainer);
                UiKit.HStack(row.gameObject, 12, TextAnchor.UpperLeft);
                var badge = UiKit.Node("Number", row);
                UiKit.Panel(badge, Palette.Primary, 14);
                UiKit.Size(badge.gameObject, 28, 28);
                var num = UiKit.Label(badge, "Label", (i + 1).ToString(), Body, 14, Palette.TextPrimary, FontStyle.Bold, TextAnchor.MiddleCenter);
                UiKit.Stretch(num.rectTransform);
                var text = UiKit.Label(row, "Text", steps[i], Body, 15, Palette.TextPrimary, lineSpacing: 1.2f);
                UiKit.Size(text.gameObject, flexibleWidth: 1);
            }
        }

        // ---------------- Pieces ----------------

        RectTransform BuildScroll(RectTransform parent, string name, float top, float bottom, out ScrollRect scroll)
        {
            var root = UiKit.Node(name, parent);
            UiKit.Stretch(root, 0, top, 0, bottom);
            scroll = root.gameObject.AddComponent<ScrollRect>();

            var viewport = UiKit.Node("Viewport", root);
            UiKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            UiKit.Panel(viewport, new Color(0, 0, 0, 0), raycast: true); // catches drags between cards

            var content = UiKit.Node("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        void Badge(RectTransform parent, string text, Vector2 corner, Vector2 offset)
        {
            var badge = UiKit.Node("ConceptArtLabel", parent);
            badge.anchorMin = badge.anchorMax = badge.pivot = corner;
            badge.anchoredPosition = offset;
            UiKit.Panel(badge, Palette.BadgeBg, 10);
            UiKit.HStack(badge.gameObject, 0, TextAnchor.MiddleCenter, 10, 10, 4, 4);
            var fit = badge.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var label = UiKit.Label(badge, "Label", text, Body, 11, Palette.TextSecondary, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        Text FactTile(RectTransform parent, string name, string caption)
        {
            var tile = UiKit.Node(name, parent);
            UiKit.Panel(tile, Palette.Surface, 12);
            UiKit.VStack(tile.gameObject, 10, 10, 10, 10, 4);
            UiKit.Size(tile.gameObject, width: 0, flexibleWidth: 1); // three equal columns, text wraps inside
            UiKit.Label(tile, "Caption", caption, Body, 10, Palette.TextSecondary, FontStyle.Bold);
            return UiKit.Label(tile, "Value", "", Body, 13, Palette.TextPrimary);
        }

        void MissingArtLabel(RectTransform frame, GameEntry g)
        {
            var label = UiKit.Label(frame, "MissingArtwork", $"Artwork missing\n{g.artworkFile}.png", Body, 14,
                Palette.TextSecondary, align: TextAnchor.MiddleCenter);
            UiKit.Stretch(label.rectTransform, 16, 16, 16, 16);
        }
    }
}
