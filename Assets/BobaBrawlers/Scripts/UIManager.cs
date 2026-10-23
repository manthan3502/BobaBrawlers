using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BobaBrawlers
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager game;
        Canvas canvas;
        RectTransform overlay, hud;
        Text timer, level, score, chargeStatus, popup, points;
        readonly Image[] hearts = new Image[3];
        ArcMeter chargeArc;
        Font displayFont, bodyFont;
        Sprite rounded, circle, heart, star;
        float popupUntil, screenOpened;
        CanvasGroup group;
        Button firstButton;
        public string CurrentScreen { get; private set; }
        public Canvas Canvas => canvas;
        public void Initialize(GameManager manager)
        {
            game = manager; displayFont = Resources.Load<Font>("Fonts/LilitaOne"); bodyFont = Resources.Load<Font>("Fonts/Barlow-Bold");
            if (!displayFont) displayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!bodyFont) bodyFont = displayFont;
            rounded = Resources.Load<Sprite>("UI/Rounded"); circle = Resources.Load<Sprite>("UI/Circle"); heart = Resources.Load<Sprite>("UI/Heart"); star = Resources.Load<Sprite>("UI/Star");
            var root = new GameObject("Responsive UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = 5; root.transform.SetParent(transform, false); canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080); scale.matchWidthOrHeight = .5f;
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            hud = Rect("Gameplay HUD", root.transform, Vector2.zero, new Vector2(1920, 1080));
            var lives = Panel(hud, new Vector2(-720, 431), new Vector2(390, 145), Palette.Navy, Palette.Blue);
            for (int i = 0; i < 3; i++) hearts[i] = Icon(lives, heart, new Vector2(-83 + i * 83, 29), new Vector2(53, 49), Palette.Orange);
            score = Label(lives, "00 / 02 KOs", new Vector2(0, -32), new Vector2(350, 44), 29, Color.white);
            var clock = Panel(hud, new Vector2(0, 437), new Vector2(330, 140), Palette.Navy, Palette.Blue);
            timer = Label(clock, "01:30", new Vector2(0, 18), new Vector2(300, 72), 64, Color.white, true);
            level = Label(clock, "FIRST SIP", new Vector2(0, -45), new Vector2(300, 35), 22, Palette.Cream);
            MakeButton(hud, "PAUSE", new Vector2(804, 451), new Vector2(225, 72), game.Pause, Palette.Blue, 26);
            points = Label(hud, "SCORE 0000", new Vector2(760, 371), new Vector2(330, 48), 27, Palette.Cream);
            var dash = Panel(hud, new Vector2(0, -444), new Vector2(470, 140), Palette.Navy, Palette.Blue);
            var arcRoot = Rect("Dash charge arc", dash, new Vector2(0, -30), new Vector2(270, 130));
            var background = arcRoot.gameObject.AddComponent<ArcMeter>(); background.color = new Color(.13f, .22f, .4f); background.thickness = 13; background.raycastTarget = false;
            var foreground = Rect("Charge", dash, new Vector2(0, -30), new Vector2(270, 130));
            chargeArc = foreground.gameObject.AddComponent<ArcMeter>(); chargeArc.thickness = 13; chargeArc.raycastTarget = false;
            chargeStatus = Label(dash, "HOLD SPACE", new Vector2(0, -28), new Vector2(435, 48), 24, Color.white);
            popup = Label(hud, "", new Vector2(0, 225), new Vector2(1000, 110), 68, Palette.Orange, true);
            overlay = Rect("Navigation", root.transform, Vector2.zero, new Vector2(1920, 1080)); group = overlay.gameObject.AddComponent<CanvasGroup>();
        }
        void Update()
        {
            if (!game || !timer) return;
            int seconds = Mathf.CeilToInt(game.Remaining);
            SetText(timer, $"{seconds / 60:00}:{seconds % 60:00}"); timer.color = seconds <= 15 ? Palette.Orange : Color.white;
            SetText(level, $"0{game.LevelIndex + 1} / {game.Level.title.ToUpperInvariant()}"); SetText(score, $"{game.Score.Knockouts:00} / {game.Level.knockoutTarget:00} KOs"); SetText(points, $"SCORE  {game.Points:0000}");
            for (int i = 0; i < 3; i++) hearts[i].color = i < game.Score.Hearts ? Palette.Orange : new Color(.25f, .3f, .43f);
            var dash = game.Player ? game.Player.Dash : null;
            if (dash)
            {
                float amount = dash.IsCharging ? dash.Charge : dash.CooldownRemaining > 0 ? 1 - dash.CooldownRemaining / (dash.cooldown + dash.dashDuration) : 1;
                chargeArc.Set(amount, dash.IsCharging && dash.Charge > .99f ? Palette.Orange : dash.CooldownRemaining > 0 ? Palette.Purple : Palette.Blue);
                SetText(chargeStatus, dash.IsCharging ? dash.Charge > .99f ? "RELEASE!" : "CHARGING..." : dash.CooldownRemaining > 0 ? "RECHARGING" : "HOLD SPACE");
            }
            if (Time.unscaledTime > popupUntil) popup.text = "";
            if (overlay.gameObject.activeSelf) group.alpha = GamePreferences.ReducedMotion ? 1 : Mathf.Clamp01((Time.unscaledTime - screenOpened) / .16f);
        }
        static void SetText(Text label, string value) { if (label.text != value) label.text = value; }
        public void Popup(string message, Color color) { popup.text = message; popup.color = color; popupUntil = Time.unscaledTime + 1.5f; }
        void Clear(string name, bool showHud = false, bool shade = true)
        {
            CurrentScreen = name; firstButton = null; screenOpened = Time.unscaledTime;
            EventSystem.current.SetSelectedGameObject(null);
            foreach (Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            hud.gameObject.SetActive(showHud); overlay.gameObject.SetActive(true); group.alpha = 1;
            if (shade) Flat(overlay, Vector2.zero, new Vector2(1920, 1080), new Color(.018f, .035f, .09f, .77f));
        }
        void Focus() { if (firstButton) EventSystem.current.SetSelectedGameObject(firstButton.gameObject); }
        RectTransform Card(string eyebrow, string title, string subtitle)
        {
            var card = Panel(overlay, new Vector2(0, -5), new Vector2(780, 850), Palette.Navy, Palette.Blue);
            Label(card, eyebrow, new Vector2(0, 350), new Vector2(700, 42), 22, Palette.Lime);
            Label(card, title, new Vector2(0, 255), new Vector2(720, 150), 67, Palette.Cream, true);
            Label(card, subtitle, new Vector2(0, 145), new Vector2(680, 75), 26, Color.white); return card;
        }
        public void ShowMenu()
        {
            Clear("Menu", false, false);
            Panel(overlay, new Vector2(0, 295), new Vector2(870, 380), new Color(.025f, .045f, .11f, .9f), Palette.Blue);
            Label(overlay, "BOBA", new Vector2(0, 365), new Vector2(900, 185), 123, Palette.Blue, true, 5);
            Label(overlay, "BRAWLERS", new Vector2(0, 258), new Vector2(1050, 145), 111, Palette.Orange, true, 5);
            Label(overlay, "Small Pearls. Big Brawls.", new Vector2(0, 150), new Vector2(760, 60), 38, Palette.Cream, true);
            for (int side = -1; side <= 1; side += 2)
            { Icon(overlay, circle, new Vector2(side * 270, 375), Vector2.one * 35, Palette.Cream); Icon(overlay, circle, new Vector2(side * 315, 335), Vector2.one * 24, Palette.Orange); }
            var card = Panel(overlay, new Vector2(0, -164), new Vector2(570, 520), new Color(.065f, .1f, .22f, .97f), Palette.Blue);
            MakeButton(card, "PLAY", new Vector2(0, 175), new Vector2(490, 92), game.PlayCampaign, Palette.Orange, 47);
            MakeButton(card, "LEVEL SELECT", new Vector2(0, 65), new Vector2(490, 72), ShowLevels, Palette.Blue);
            MakeButton(card, "CHOOSE YOUR PEARL", new Vector2(0, -28), new Vector2(490, 68), ShowCharacters, Palette.Blue, 29);
            MakeButton(card, "HOW TO PLAY", new Vector2(0, -116), new Vector2(490, 65), ShowHowTo, Palette.Blue, 29);
            MakeButton(card, "SETTINGS", new Vector2(-126, -204), new Vector2(238, 62), () => ShowSettings(false), Palette.Blue, 26);
            MakeButton(card, "EXIT", new Vector2(126, -204), new Vector2(238, 62), game.Exit, Palette.Purple, 26);
            Label(overlay, "3 ROUNDS  /  ONE CHAMPION", new Vector2(0, -480), new Vector2(600, 45), 24, Palette.Navy);
            Label(overlay, $"YOUR PEARL\n{GamePreferences.SkinNames[GamePreferences.Skin]}", new Vector2(-660, -380), new Vector2(460, 80), 28, Color.white);
            Label(overlay, $"BEST STREAK\n{GamePreferences.BestCombo}x SWEET COMBO", new Vector2(660, -380), new Vector2(460, 80), 28, Palette.Lime); Focus();
        }
        public void ShowHud() { Clear("HUD", true, false); popup.text = ""; popupUntil = 0; overlay.gameObject.SetActive(false); }
        public void ShowPause()
        {
            Clear("Pause", true); var card = Card("TAKE A BREATHER", "PAUSED", "Your tea can wait.");
            MakeButton(card, "RESUME", new Vector2(0, 30), new Vector2(550, 83), game.Resume, Palette.Orange, 36);
            MakeButton(card, "RESTART", new Vector2(0, -80), new Vector2(550, 72), game.StartMatch, Palette.Blue);
            MakeButton(card, "SETTINGS", new Vector2(0, -180), new Vector2(550, 72), () => ShowSettings(true), Palette.Blue);
            MakeButton(card, "MAIN MENU", new Vector2(0, -280), new Vector2(550, 72), game.ToMenu, Palette.Purple); Focus();
        }
        public void ShowResult(bool victory)
        {
            bool champion = victory && game.LevelIndex == 2; Clear(victory ? champion ? "Champion" : "Victory" : "Defeat");
            var card = Card(champion ? "THE CUP IS YOURS" : victory ? game.Level.title.ToUpperInvariant() + " COMPLETE" : game.Score.Hearts == 0 ? "FAHHH! ALL HEARTS SPILLED" : "ONE MORE ROUND?", champion ? "BOBA\nCHAMPION!" : victory ? "SWEET\nVICTORY!" : "SPILLED\nTHE TEA!", $"{game.Score.Knockouts} / {game.Level.knockoutTarget} KOs   |   {game.Score.Hearts} lives   |   {Mathf.CeilToInt(game.Remaining)}s left");
            Label(card, $"SCORE {game.Points:0000}    /    BEST {GamePreferences.Best(game.LevelIndex):0000}", new Vector2(0, 61), new Vector2(710, 55), 31, Palette.Lime, true);
            Label(card, victory ? champion ? "Three rounds. One very sweet victory." : "The next brew is ready. Keep the streak alive." : game.Score.Hearts == 0 ? "All three lives lost. Dash smart. Try again." : "Time ran out. Aim for the hole or the edge.", new Vector2(0, -4), new Vector2(680, 65), 25, Palette.Cream);
            if (victory && !champion) MakeButton(card, "NEXT LEVEL", new Vector2(0, -102), new Vector2(550, 80), game.NextLevel, Palette.Orange, 36);
            MakeButton(card, "RETRY", new Vector2(0, victory && !champion ? -207 : -125), new Vector2(550, 72), game.StartMatch, victory && !champion ? Palette.Blue : Palette.Orange);
            MakeButton(card, "MAIN MENU", new Vector2(0, victory && !champion ? -304 : -225), new Vector2(550, 72), game.ToMenu, Palette.Blue);
            if (champion) for (int i = 0; i < 5; i++) Icon(overlay, star, new Vector2(-600 + i * 300, 430 - Mathf.Abs(i - 2) * 35), Vector2.one * (i == 2 ? 85 : 58), Palette.Orange); Focus();
        }
        public void ShowLevels()
        {
            Clear("Levels"); Label(overlay, "PICK YOUR BREW", new Vector2(0, 423), new Vector2(1500, 110), 79, Palette.Cream, true);
            Label(overlay, "Win a round to unlock the next. Your best scores stay with you.", new Vector2(0, 335), new Vector2(1550, 55), 28, Color.white);
            for (int i = 0; i < 3; i++)
            {
                int index = i; var definition = game.levels[i]; bool unlocked = i < GamePreferences.Unlocked;
                var card = Panel(overlay, new Vector2((i - 1) * 550, -20), new Vector2(500, 580), Palette.Navy, unlocked ? definition.theme : new Color(.25f, .3f, .4f));
                Label(card, "ROUND 0" + (i + 1), new Vector2(0, 235), new Vector2(440, 45), 26, definition.theme);
                var preview = Panel(card, new Vector2(0, 120), new Vector2(390, 170), definition.theme * .65f, definition.theme);
                Icon(preview, circle, new Vector2(0, -8), new Vector2(310, 120), Palette.Cream); Icon(preview, circle, new Vector2(0, -8), new Vector2(74, 35), Palette.Navy);
                for (int j = 0; j <= i; j++) Icon(preview, circle, new Vector2(-95 + j * 75, 28 - j * 12), Vector2.one * 30, j % 2 == 0 ? Palette.Purple : Palette.Lime);
                Label(card, definition.title.ToUpperInvariant(), new Vector2(0, -8), new Vector2(465, 70), 43, Palette.Cream, true);
                Label(card, $"{definition.difficulty}\n{definition.opponents} opponent{(i > 0 ? "s" : "")}  /  {definition.knockoutTarget} KOs  /  {definition.duration:0}s", new Vector2(0, -95), new Vector2(460, 90), 27, Color.white);
                Label(card, unlocked ? $"BEST  {GamePreferences.Best(i):0000}" : "WIN THE PREVIOUS ROUND TO UNLOCK", new Vector2(0, -170), new Vector2(455, 50), unlocked ? 25 : 20, unlocked ? Palette.Lime : Palette.Cream);
                var button = MakeButton(card, unlocked ? "PLAY ROUND " + (i + 1) : "LOCKED", new Vector2(0, -235), new Vector2(410, 72), () => game.StartLevel(index), definition.theme); button.interactable = unlocked;
            }
            MakeButton(overlay, "BACK", new Vector2(0, -430), new Vector2(360, 70), ShowMenu, Palette.Blue); Focus();
        }
        public void ShowCharacters()
        {
            Clear("Characters"); Label(overlay, "CHOOSE YOUR PEARL", new Vector2(0, 420), new Vector2(1600, 110), 78, Palette.Cream, true);
            Label(overlay, "Same moves. Same power. Your own flavor.", new Vector2(0, 335), new Vector2(1300, 60), 29, Color.white);
            for (int i = 0; i < 3; i++)
            {
                int index = i; Color tint = GamePreferences.SkinColors[i]; bool selected = i == GamePreferences.Skin;
                var card = Panel(overlay, new Vector2((i - 1) * 550, -15), new Vector2(500, 575), Palette.Navy, selected ? Palette.Lime : tint);
                var portrait = Rect("Live 3D model", card, new Vector2(0, 85), new Vector2(370, 330));
                portrait.gameObject.AddComponent<RawImage>().raycastTarget = false;
                portrait.gameObject.AddComponent<CharacterPortrait>().Initialize(game.playerPrefab, i);
                Label(card, GamePreferences.SkinNames[i], new Vector2(0, -110), new Vector2(460, 70), 39, Palette.Cream, true);
                Label(card, selected ? "YOUR CURRENT FIGHTER" : new[] { "PEARL / BOXING GLOVES", "JELLY / MANGO LEAF", "TEA CUP / NINJA HEADBAND" }[i], new Vector2(0, -174), new Vector2(460, 40), 23, selected ? Palette.Lime : Color.white);
                MakeButton(card, selected ? "SELECTED" : "SELECT " + (i + 1), new Vector2(0, -235), new Vector2(410, 70), () => { GamePreferences.Skin = index; if (game.Presentation) game.Presentation.PreviewSkin(index); ShowCharacters(); }, selected ? Palette.Lime : tint);
            }
            MakeButton(overlay, "READY / BACK", new Vector2(0, -430), new Vector2(430, 70), ShowMenu, Palette.Blue); Focus();
        }
        void Portrait(Transform parent, Vector2 at, float size, Color tint)
        {
            Icon(parent, circle, at + new Vector2(0, -10), Vector2.one * (size + 14), Palette.Blue * .4f); Icon(parent, circle, at, Vector2.one * size, tint);
            Icon(parent, circle, at + new Vector2(-size * .2f, size * .24f), Vector2.one * (size * .2f), new Color(1, 1, 1, .25f));
            Flat(parent, at + new Vector2(0, size * .22f), new Vector2(size * .94f, size * .09f), Palette.Orange);
            for (int side = -1; side <= 1; side += 2)
            { Icon(parent, circle, at + new Vector2(side * size * .18f, 0), new Vector2(size * .1f, size * .15f), Palette.Navy); Icon(parent, circle, at + new Vector2(side * size * .18f - 3, 5), Vector2.one * (size * .035f), Color.white); }
            Icon(parent, circle, at + new Vector2(0, -size * .2f), new Vector2(size * .12f, size * .08f), Palette.Navy);
        }
        public void ShowHowTo()
        {
            Clear("HowTo"); var card = Card("A LITTLE PEARL. A BIG PUSH.", "HOW TO BRAWL", "Push ingredients into the vortex or over the edge.");
            Label(card, "WASD / ARROWS  :  Move & aim\n\nHOLD SPACE  :  Charge your dash\nRELEASE SPACE  :  Send it!\n\nESC  :  Pause / resume\n\nReach the KO target before time runs out.\nKeep your three hearts. Flashing fighters\nhave brief respawn protection.", new Vector2(0, -93), new Vector2(680, 380), 27, Color.white);
            MakeButton(card, "GOT IT", new Vector2(0, -330), new Vector2(550, 73), ShowMenu, Palette.Orange); Focus();
        }
        public void ShowSettings(bool paused)
        {
            Clear("Settings", paused); var card = Card("MAKE YOURSELF COMFORTABLE", "SETTINGS", "Your preferences are saved on this computer.");
            Slider(card, "SOUND EFFECTS", 43, game.Feedback.SfxVolume, game.Feedback.SetSfx); Slider(card, "MUSIC", -71, game.Feedback.MusicVolume, game.Feedback.SetMusic);
            Toggle(card, "FULLSCREEN", -163, GamePreferences.Fullscreen, v => GamePreferences.Fullscreen = v); Toggle(card, "SCREEN SHAKE", -224, GamePreferences.Shake, v => GamePreferences.Shake = v);
            Toggle(card, "REDUCED MOTION", -285, GamePreferences.ReducedMotion, v => GamePreferences.ReducedMotion = v);
            MakeButton(card, "BACK", new Vector2(0, -365), new Vector2(550, 65), () => { PlayerPrefs.Save(); if (paused) ShowPause(); else ShowMenu(); }, Palette.Blue); Focus();
        }
        void Toggle(Transform parent, string title, float y, bool value, Action<bool> action)
        {
            Label(parent, title, new Vector2(-95, y), new Vector2(390, 50), 27, Color.white);
            var rt = Panel(parent, new Vector2(230, y), new Vector2(75, 45), new Color(.1f, .16f, .3f), Palette.Blue); rt.name = title;
            var mark = Icon(rt, star, Vector2.zero, new Vector2(28, 28), Palette.Lime);
            var toggle = rt.gameObject.AddComponent<UnityEngine.UI.Toggle>(); toggle.targetGraphic = rt.GetComponent<Image>(); toggle.graphic = mark; toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => { action(v); game.Feedback.Button(); });
        }
        void Slider(Transform parent, string title, float y, float value, Action<float> action)
        {
            Label(parent, title, new Vector2(0, y + 38), new Vector2(600, 40), 26, Color.white);
            var track = Panel(parent, new Vector2(0, y), new Vector2(580, 21), new Color(.1f, .16f, .3f), Palette.Blue); track.name = title;
            var handle = Rect("Handle", track, Vector2.zero, new Vector2(35, 45)); var image = handle.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = Palette.Orange;
            var slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.handleRect = handle; slider.targetGraphic = image; slider.minValue = 0; slider.maxValue = 1; slider.value = value; slider.onValueChanged.AddListener(v => action(v));
        }
        RectTransform Rect(string name, Transform parent, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = at; rt.sizeDelta = size; return rt;
        }
        RectTransform Flat(Transform parent, Vector2 at, Vector2 size, Color color) { var rt = Rect("Surface", parent, at, size); rt.gameObject.AddComponent<Image>().color = color; return rt; }
        RectTransform Panel(Transform parent, Vector2 at, Vector2 size, Color color, Color edge)
        {
            var rt = Rect("Panel", parent, at, size); var image = rt.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
            var outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(edge.r, edge.g, edge.b, .8f); outline.effectDistance = new Vector2(2, -2); return rt;
        }
        Image Icon(Transform parent, Sprite sprite, Vector2 at, Vector2 size, Color color) { var rt = Rect("Icon", parent, at, size); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image; }
        Text Label(Transform parent, string text, Vector2 at, Vector2 size, int fontSize, Color color, bool display = false, int border = 0)
        {
            var rt = Rect(text.Length > 28 ? "Text" : text, parent, at, size); var label = rt.gameObject.AddComponent<Text>(); label.font = display ? displayFont : bodyFont; label.text = text; label.fontSize = fontSize; label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            if (display) label.verticalOverflow = VerticalWrapMode.Overflow;
            if ((display && color != Palette.Navy) || border > 0) { var outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.025f, .045f, .1f, .95f); outline.effectDistance = Vector2.one * (border > 0 ? border : 2); } return label;
        }
        Button MakeButton(Transform parent, string title, Vector2 at, Vector2 size, Action action, Color color, int fontSize = 32)
        {
            var rt = Panel(parent, at, size, new Color(.025f, .045f, .12f), Palette.Blue); rt.name = title;
            var face = Panel(rt, new Vector2(0, 5), size - new Vector2(4, 5), color, new Color(1, 1, 1, .45f));
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = face.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.selectedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(.75f, .75f, .75f); colors.disabledColor = new Color(.3f, .35f, .45f, .8f); button.colors = colors;
            Label(face, title, Vector2.zero, size - new Vector2(24, 12), fontSize, color == Palette.Orange || color == Palette.Lime ? Palette.Navy : Color.white, true);
            button.onClick.AddListener(() => { game.Feedback.Button(); action(); }); rt.gameObject.AddComponent<ButtonMotion>(); if (!firstButton) firstButton = button; return button;
        }
    }
}

