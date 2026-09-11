using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SlimeKeyViewer
{
    internal static class FlatGui
    {
        public static readonly Color WindowPlate = Hex(0x1C1C22, 0.97f);
        public static readonly Color Card = new Color(1f, 1f, 1f, 0.055f);
        public static readonly Color Field = new Color(0f, 0f, 0f, 0.28f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color Chip = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color Button = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color ButtonHover = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color Text = Hex(0xF5F5F7, 1f);
        public static readonly Color Muted = Hex(0xEBEBF5, 0.60f);
        public static readonly Color Faint = Hex(0xEBEBF5, 0.38f);
        public static readonly Color Accent = Hex(0x0A84FF, 1f);
        public static readonly Color SwitchOff = new Color(1f, 1f, 1f, 0.18f);

        public const float WindowWidth = 460f;
        public const float HeaderHeight = 84f;
        public const float Padding = 14f;
        public const float RowHeight = 32f;
        public const float TabHeight = 30f;
        public const float TabWidth = 92f;

        private const float LabelWidth = 132f;
        private const float ValueWidth = 52f;

        private static readonly List<Texture2D> Textures = new List<Texture2D>();
        private static bool _built;
        private static bool _cardFirstRow;

        private static Texture2D _texLine, _texField, _texAccent, _texKnob, _texSwitchOff, _texWhite;

        public static string Scope = string.Empty;

        private static int _rowId;

        private static string _editingId;
        private static string _editingText = string.Empty;

        public static GUIStyle Window { get; private set; }
        public static GUIStyle Title { get; private set; }
        public static GUIStyle Heading { get; private set; }
        public static GUIStyle CardBox { get; private set; }
        public static GUIStyle Label { get; private set; }
        public static GUIStyle Value { get; private set; }
        public static GUIStyle Hint { get; private set; }
        public static GUIStyle Button2 { get; private set; }
        public static GUIStyle TabLabel { get; private set; }
        private static GUIStyle ValueField { get; set; }
        private static GUIStyle Invisible { get; set; }
        private static GUIStyle Knob { get; set; }
        private static GUIStyle TabRail { get; set; }
        private static GUIStyle TabChip { get; set; }

        private static Color Hex(int rgb, float a)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
        }

        public static void EnsureStyles()
        {
            if (_built) return;
            _built = true;

            _texLine = Flat(Line);
            _texField = Flat(Field);
            _texAccent = Flat(Accent);
            _texSwitchOff = Flat(SwitchOff);
            _texWhite = Flat(Color.white);
            _texKnob = Circle(18, Color.white);

            Window = Panel(14, WindowPlate, new RectOffset(0, 0, 0, 0));
            CardBox = Panel(12, Card, new RectOffset((int)Padding, (int)Padding, 4, 4));
            TabRail = Panel(11, Field, new RectOffset(3, 3, 3, 3));
            TabChip = Panel(7, Chip, new RectOffset(0, 0, 0, 0));

            Title = Text2(15, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            Heading = Text2(11, FontStyle.Bold, Faint, TextAnchor.MiddleLeft);
            Heading.margin = new RectOffset(4, 0, 10, 4);
            Label = Text2(13, FontStyle.Normal, Text, TextAnchor.MiddleLeft);
            Value = Text2(12, FontStyle.Normal, Muted, TextAnchor.MiddleRight);
            Hint = Text2(11, FontStyle.Normal, Faint, TextAnchor.UpperLeft);
            Hint.wordWrap = true;
            Hint.margin = new RectOffset((int)Padding, (int)Padding, 0, 6);
            TabLabel = Text2(12, FontStyle.Normal, Muted, TextAnchor.MiddleCenter);

            Button2 = Panel(7, Button, new RectOffset(10, 10, 0, 0));
            Button2.fontSize = 12;
            Button2.alignment = TextAnchor.MiddleCenter;
            Button2.fixedHeight = 24f;
            Button2.margin = new RectOffset(0, 0, 4, 4);
            Button2.normal.textColor = Text;
            Button2.hover.background = Round(7, ButtonHover, out _);
            Button2.hover.textColor = Text;
            Button2.active.background = Round(7, Accent, out _);
            Button2.active.textColor = Color.white;

            ValueField = Panel(5, Field, new RectOffset(5, 5, 0, 0));
            ValueField.fontSize = 12;
            ValueField.alignment = TextAnchor.MiddleRight;
            ValueField.normal.textColor = Text;
            ValueField.focused.background = ValueField.normal.background;
            ValueField.focused.textColor = Text;
            ValueField.hover.background = ValueField.normal.background;
            ValueField.hover.textColor = Text;
            ValueField.active.background = ValueField.normal.background;
            ValueField.active.textColor = Text;

            Invisible = new GUIStyle();

            Knob = new GUIStyle
            {
                fixedWidth = 18f,
                fixedHeight = 18f,
                normal = { background = _texKnob },
                active = { background = _texKnob }
            };
        }

        private static GUIStyle Panel(int radius, Color color, RectOffset padding)
        {
            int border;
            var tex = Round(radius, color, out border);
            return new GUIStyle
            {
                normal = { background = tex },
                border = new RectOffset(border, border, border, border),
                padding = padding,
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        private static GUIStyle Text2(int size, FontStyle style, Color color, TextAnchor anchor)
        {
            return new GUIStyle
            {
                fontSize = size,
                fontStyle = style,
                alignment = anchor,
                normal = { textColor = color },
                wordWrap = false
            };
        }

        private static Texture2D Round(int radius, Color color, out int border)
        {
            var tex = RoundedBox.Solid(radius, color, out border);
            Textures.Add(tex);
            return tex;
        }

        private static Texture2D Circle(int diameter, Color color)
        {
            var tex = RoundedBox.Circle(diameter, color);
            Textures.Add(tex);
            return tex;
        }

        private static Texture2D Flat(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            Textures.Add(tex);
            return tex;
        }

        public static void GroupHeading(string text) => GUILayout.Label(text, Heading);

        public static void BeginCard()
        {
            GUILayout.BeginVertical(CardBox);
            _cardFirstRow = true;
        }

        public static void EndCard() { GUILayout.EndVertical(); GUILayout.Space(8f); }

        public static void BeginPage(string scope)
        {
            Scope = scope;
            _rowId = 0;
        }

        public static Rect Row(float height)
        {
            _rowId++;
            if (!_cardFirstRow)
            {
                var line = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(line, _texLine);
            }
            _cardFirstRow = false;
            return GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
        }

        public static void FullDivider(Rect r) => GUI.DrawTexture(r, _texLine);

        public static int TabRailControl(Rect area, string[] tabs, int selected)
        {
            var railWidth = tabs.Length * TabWidth + (tabs.Length - 1) * 2f + 6f;
            var rail = new Rect(area.x + (area.width - railWidth) * 0.5f, area.y, railWidth, TabHeight);
            GUI.Box(rail, GUIContent.none, TabRail);

            for (var i = 0; i < tabs.Length; i++)
            {
                var chip = new Rect(rail.x + 3f + i * (TabWidth + 2f), rail.y + 3f, TabWidth, TabHeight - 6f);
                if (i == selected) GUI.Box(chip, GUIContent.none, TabChip);

                TabLabel.normal.textColor = i == selected ? Text : Muted;
                GUI.Label(chip, tabs[i], TabLabel);

                if (GUI.Button(chip, GUIContent.none, Invisible)) selected = i;
            }
            return selected;
        }

        public static bool Slider(string label, string key, ref float value, float min, float max, string format)
        {
            var row = Row(RowHeight);
            GUI.Label(new Rect(row.x, row.y, LabelWidth, row.height), label, Label);

            var trackX = row.x + LabelWidth;
            var trackW = Mathf.Max(40f, row.width - LabelWidth - ValueWidth - 10f);
            var track = new Rect(trackX, row.y + (row.height - 18f) * 0.5f, trackW, 18f);

            var railY = track.y + 7f;
            GUI.DrawTexture(new Rect(track.x, railY, track.width, 4f), _texField);
            var t = Mathf.InverseLerp(min, max, value);
            if (t > 0f) GUI.DrawTexture(new Rect(track.x, railY, track.width * t, 4f), _texAccent);

            var next = GUI.HorizontalSlider(track, value, min, max, Invisible, Knob);
            next = NumberField(row, key, next, min, max, format);

            if (Mathf.Approximately(value, next)) return false;
            value = next;
            return true;
        }

        private static float NumberField(Rect row, string key, float value, float min, float max, string format)
        {
            var rect = new Rect(row.xMax - ValueWidth, row.y + (row.height - 20f) * 0.5f, ValueWidth, 20f);
            var id = Scope + key;
            var mine = _editingId == id;

            GUI.SetNextControlName(id);
            var shown = mine ? _editingText : value.ToString(format, CultureInfo.InvariantCulture);
            var typed = GUI.TextField(rect, shown, 8, ValueField);

            if (GUI.GetNameOfFocusedControl() != id)
            {
                if (mine) _editingId = null;
                return value;
            }

            if (!mine) _editingText = shown;
            _editingText = typed;
            _editingId = id;

            float parsed;
            if (float.TryParse(_editingText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) &&
                !float.IsNaN(parsed) && !float.IsInfinity(parsed))
                return Mathf.Clamp(parsed, min, max);

            return value;
        }

        public static bool AnchorGrid(string label, ref ViewerAnchor value)
        {
            const float cell = 22f, gap = 3f;
            const float grid = cell * 3f + gap * 2f;

            var row = Row(grid + 10f);
            GUI.Label(new Rect(row.x, row.y, Mathf.Max(0f, row.width - grid - 10f), row.height), label, Label);

            var originX = row.xMax - grid;
            var originY = row.y + (row.height - grid) * 0.5f;

            var changed = false;
            var e = Event.current;

            for (var i = 0; i < 9; i++)
            {
                var box = new Rect(originX + i % 3 * (cell + gap), originY + i / 3 * (cell + gap), cell, cell);

                if (e.type == EventType.MouseDown && e.button == 0 && box.Contains(e.mousePosition))
                {
                    value = (ViewerAnchor)i;
                    changed = true;
                    GUI.changed = true;
                    e.Use();
                }

                GUI.DrawTexture(box, (int)value == i ? _texAccent : _texField, ScaleMode.StretchToFill,
                                true, 0f, Color.white, 0f, 5f);
            }

            return changed;
        }

        public static bool ColorRow(string label, string key, ref Color32 value)
        {
            var row = Row(RowHeight);

            const float swatchW = 46f, swatchH = 20f, fieldW = 80f, gap = 8f;

            var swatch = new Rect(row.xMax - swatchW, row.y + (row.height - swatchH) * 0.5f, swatchW, swatchH);
            var field = new Rect(swatch.x - gap - fieldW, row.y + (row.height - 20f) * 0.5f, fieldW, 20f);

            GUI.Label(new Rect(row.x, row.y, Mathf.Max(0f, field.x - row.x - 6f), row.height), label, Label);

            var next = HexField(field, key, value);

            GUI.DrawTexture(swatch, _texWhite, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 4f);
            GUI.DrawTexture(swatch, _texWhite, ScaleMode.StretchToFill, true, 0f, (Color)next, 0f, 4f);

            if (next.r == value.r && next.g == value.g && next.b == value.b && next.a == value.a) return false;

            value = next;
            return true;
        }

        private static Color32 HexField(Rect rect, string key, Color32 value)
        {
            var id = Scope + key;
            var mine = _editingId == id;

            GUI.SetNextControlName(id);
            var shown = mine ? _editingText : SlimeConfig.ColorToHex(value);
            var typed = GUI.TextField(rect, shown, 9, ValueField);

            if (GUI.GetNameOfFocusedControl() != id)
            {
                if (mine) _editingId = null;
                return value;
            }

            if (!mine) _editingText = shown;
            _editingText = typed;
            _editingId = id;

            Color32 parsed;
            return SlimeConfig.TryParseColor(_editingText, value.a, out parsed) ? parsed : value;
        }

        public static bool Toggle(string label, ref bool value)
        {
            var row = Row(RowHeight);

            const float trackW = 42f, trackH = 22f, knob = 18f;
            var track = new Rect(row.xMax - trackW, row.y + (row.height - trackH) * 0.5f, trackW, trackH);

            var changed = false;
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && row.Contains(e.mousePosition))
            {
                value = !value;
                changed = true;
                GUI.changed = true;
                e.Use();
            }

            GUI.Label(new Rect(row.x, row.y, Mathf.Max(0f, row.width - trackW - 10f), row.height), label, Label);
            GUI.DrawTexture(track, value ? _texAccent : _texSwitchOff, ScaleMode.StretchToFill, true, 0f,
                            Color.white, 0f, trackH * 0.5f);
            var knobX = value ? track.xMax - knob - 2f : track.x + 2f;
            GUI.DrawTexture(new Rect(knobX, track.y + 2f, knob, knob), _texKnob);

            return changed;
        }

        public static void Release()
        {
            foreach (var tex in Textures)
                if (tex != null) Object.Destroy(tex);

            Textures.Clear();
            _built = false;
            Window = Title = Heading = CardBox = Label = Value = Hint = Button2 = TabLabel = null;
            Invisible = Knob = TabRail = TabChip = null;
            _texWhite = null;
        }
    }

    internal static class SettingsPage
    {
        public static string[] Tabs => new[] { Lang.S.TabKeys, Lang.S.TabBehaviour };

        private const int EditNone = -1;
        private const int EditAll = -2;

        private static bool _capturing;

        private static int _pendingRemove = -1;
        private static bool _hasPendingAdd;
        private static KeyCode _pendingAdd;
        private static bool _showCapture, _showSlimeShadow, _showLabelShadow, _showFlipAuto, _showFade;

        private static int _editIndex = EditNone;
        private static int _shownEdit = EditNone;

        public static void Draw(int tab)
        {
            var cfg = ConfigStore.Current;
            var dirty = false;

            if (_pendingRemove >= 0)
            {
                if (_pendingRemove < cfg.Keys.Count) cfg.Keys.RemoveAt(_pendingRemove);
                _pendingRemove = -1;
                dirty = true;
            }
            if (_hasPendingAdd)
            {
                _hasPendingAdd = false;
                if (!cfg.HasKey(_pendingAdd)) { cfg.Keys.Add(cfg.NewKey(_pendingAdd)); dirty = true; }
            }

            if (Event.current.type == EventType.Layout)
            {
                _showCapture = _capturing;

                _showFlipAuto = cfg.FlipOnPrep;
                _showFade = cfg.SlimeVisibleOnlyOnInput;

                if (_editIndex >= 0 && _editIndex >= cfg.Keys.Count) _editIndex = EditNone;
                _shownEdit = _editIndex;
                var edited = Edited(cfg);
                _showSlimeShadow = edited != null && edited.SlimeShadow;
                _showLabelShadow = edited != null && edited.LabelShadow;
            }

            FlatGui.BeginPage("g/");
            if (tab == 0)
                dirty |= _shownEdit == EditNone ? DrawKeyList(cfg) : DrawStyleEditor(cfg);
            else
                dirty |= DrawBehaviourTab(cfg);

            if (dirty) ConfigStore.MarkChanged();
        }

        private static KeyStyle Edited(SlimeConfig cfg)
        {
            if (_shownEdit == EditAll) return cfg.Template;
            if (_shownEdit >= 0 && _shownEdit < cfg.Keys.Count) return cfg.Keys[_shownEdit];
            return null;
        }

        private static bool DrawKeyList(SlimeConfig cfg)
        {
            var dirty = false;

            FlatGui.GroupHeading(string.Format(Lang.S.KeysHeading, cfg.Keys.Count));
            if (cfg.Keys.Count > 0) FlatGui.BeginCard();

            for (var i = 0; i < cfg.Keys.Count; i++)
            {
                var row = FlatGui.Row(FlatGui.RowHeight);
                GUI.Label(new Rect(row.x, row.y, 84f, row.height), KeyNames.Of(cfg.Keys[i].Key), FlatGui.Label);

                const float arrow = 26f, edit = 48f, del = 48f, gap = 4f;
                var y = row.y + (row.height - 24f) * 0.5f;
                var x = row.xMax - del;

                if (GUI.Button(new Rect(x, y, del, 24f), Lang.S.Delete, FlatGui.Button2))
                    _pendingRemove = i;

                x -= edit + gap;
                if (GUI.Button(new Rect(x, y, edit, 24f), Lang.S.Edit, FlatGui.Button2)) _editIndex = i;

                x -= arrow + gap;
                if (GUI.Button(new Rect(x, y, arrow, 24f), "▶", FlatGui.Button2) && i < cfg.Keys.Count - 1)
                {
                    Swap(cfg.Keys, i, i + 1);
                    dirty = true;
                }

                x -= arrow;
                if (GUI.Button(new Rect(x, y, arrow, 24f), "◀", FlatGui.Button2) && i > 0)
                {
                    Swap(cfg.Keys, i, i - 1);
                    dirty = true;
                }
            }

            if (cfg.Keys.Count > 0) FlatGui.EndCard();

            if (_showCapture)
            {
                GUILayout.Label(Lang.S.CaptureHint, FlatGui.Hint);
                Capture(cfg);
                if (GUILayout.Button(Lang.S.Cancel, FlatGui.Button2, GUILayout.Width(90f))) _capturing = false;
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Lang.S.AddKey, FlatGui.Button2, GUILayout.Width(90f))) _capturing = true;
                GUILayout.Space(8f);
                if (GUILayout.Button(Lang.S.EditAllKeys, FlatGui.Button2, GUILayout.Width(120f))) _editIndex = EditAll;
                GUILayout.EndHorizontal();
            }

            return dirty;
        }

        private static bool DrawStyleEditor(SlimeConfig cfg)
        {
            var style = Edited(cfg);
            if (style == null) { _editIndex = EditNone; return false; }

            var all = _shownEdit == EditAll;
            var dirty = false;
            FlatGui.BeginPage(_shownEdit + "/");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Lang.S.Back, FlatGui.Button2, GUILayout.Width(80f))) _editIndex = EditNone;
            GUILayout.FlexibleSpace();
            GUILayout.Label(all ? Lang.S.AllKeys : string.Format(Lang.S.OneKey, KeyNames.Of(style.Key)), FlatGui.Title);
            GUILayout.EndHorizontal();

            FlatGui.GroupHeading(Lang.S.GroupBox);
            FlatGui.BeginCard();
            dirty |= FlatGui.Slider(Lang.S.Width, "boxW", ref style.BoxWidth, 16f, 200f, "0");
            dirty |= FlatGui.Slider(Lang.S.Height, "boxH", ref style.BoxHeight, 16f, 200f, "0");
            dirty |= FlatGui.Slider(Lang.S.GapToNext, "gap", ref style.BoxSpacing, 0f, 80f, "0");
            dirty |= FlatGui.Slider(Lang.S.CornerRadius, "radius", ref style.BoxCornerRadius,
                                    0f, Mathf.Max(1f, Mathf.Min(style.BoxWidth, style.BoxHeight) * 0.5f - 1f), "0");
            dirty |= FlatGui.Slider(Lang.S.BorderWidth, "border", ref style.BoxBorderWidth, 0f, 20f, "0");
            dirty |= FlatGui.Slider(Lang.S.FontSize, "font", ref style.BoxFontSize, 6f, 120f, "0");
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupSlime);
            FlatGui.BeginCard();
            dirty |= FlatGui.Slider(Lang.S.Size, "slimeSize", ref style.SlimeScale, 0.02f, 1f, "0.00");
            dirty |= FlatGui.Slider(Lang.S.GapFromBox, "slimeGap", ref style.SlimeGap, -40f, 120f, "0");
            dirty |= FlatGui.Toggle(Lang.S.Shadow, ref style.SlimeShadow);
            if (_showSlimeShadow)
            {
                dirty |= FlatGui.Slider(Lang.S.OffsetX, "shX", ref style.SlimeShadowX, -60f, 60f, "0");
                dirty |= FlatGui.Slider(Lang.S.OffsetY, "shY", ref style.SlimeShadowY, -60f, 60f, "0");
                dirty |= FlatGui.Slider(Lang.S.Opacity, "shA", ref style.SlimeShadowAlpha, 0f, 1f, "0.00");
            }
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupLabelShadow);
            FlatGui.BeginCard();
            dirty |= FlatGui.Toggle(Lang.S.Shadow, ref style.LabelShadow);
            if (_showLabelShadow)
            {
                dirty |= FlatGui.Slider(Lang.S.Distance, "lsDist", ref style.LabelShadowDistance, 0f, 1f, "0.00");
                dirty |= FlatGui.Slider(Lang.S.Softness, "lsSoft", ref style.LabelShadowSoftness, 0f, 1f, "0.00");
            }
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupColorsIdle);
            FlatGui.BeginCard();
            dirty |= FlatGui.ColorRow(Lang.S.ColorBorder, "cbi", ref style.BorderIdle);
            dirty |= FlatGui.ColorRow(Lang.S.ColorFill, "cfi", ref style.FillIdle);
            dirty |= FlatGui.ColorRow(Lang.S.ColorText, "cti", ref style.TextIdle);
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupColorsPressed);
            FlatGui.BeginCard();
            dirty |= FlatGui.ColorRow(Lang.S.ColorBorder, "cbp", ref style.BorderPressed);
            dirty |= FlatGui.ColorRow(Lang.S.ColorFill, "cfp", ref style.FillPressed);
            dirty |= FlatGui.ColorRow(Lang.S.ColorText, "ctp", ref style.TextPressed);
            FlatGui.EndCard();

            if (dirty && all) cfg.ApplyTemplateToAll();
            return dirty;
        }

        private static bool DrawBehaviourTab(SlimeConfig cfg)
        {
            var dirty = false;

            FlatGui.GroupHeading(Lang.S.GroupJump);
            FlatGui.BeginCard();
            var onPress = cfg.JumpTrigger == JumpTrigger.OnPress;
            if (FlatGui.Toggle(Lang.S.JumpOnPress, ref onPress))
            {
                cfg.JumpTrigger = onPress ? JumpTrigger.OnPress : JumpTrigger.OnRelease;
                dirty = true;
            }
            dirty |= FlatGui.Slider(Lang.S.JumpHeight, "jumpH", ref cfg.JumpRise, 0f, 600f, "0");
            dirty |= FlatGui.Slider(Lang.S.StepDuration, "step", ref cfg.StepDuration, 0.05f, 1f, "0.00");
            dirty |= FlatGui.Toggle(Lang.S.FlipEachJump, ref cfg.FlipOnPrep);
            if (_showFlipAuto)
            {
                dirty |= FlatGui.Toggle(Lang.S.FlipOnLanding, ref cfg.FlipOnAutoReturn);
            }
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupParticle);
            FlatGui.BeginCard();
            dirty |= FlatGui.Toggle(Lang.S.ParticleAtFeet, ref cfg.ParticleAtFeet);
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupVisibility);
            FlatGui.BeginCard();
            dirty |= FlatGui.Toggle(Lang.S.VisibleOnlyOnInput, ref cfg.SlimeVisibleOnlyOnInput);
            if (_showFade)
                dirty |= FlatGui.Slider(Lang.S.FadeDuration, "fade", ref cfg.SlimeFadeDuration, 0.02f, 1f, "0.00");
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupPlacement);
            FlatGui.BeginCard();
            dirty |= FlatGui.Slider(Lang.S.PosX, "posX", ref cfg.PosX, 0f, 1f, "0.00");
            dirty |= FlatGui.Slider(Lang.S.PosY, "posY", ref cfg.PosY, 0f, 1f, "0.00");
            dirty |= FlatGui.AnchorGrid(Lang.S.Anchor, ref cfg.Anchor);
            dirty |= FlatGui.Slider(Lang.S.OverallSize, "size", ref cfg.Size, 0.2f, 2f, "0.00");
            FlatGui.EndCard();

            FlatGui.GroupHeading(Lang.S.GroupDisplay);
            FlatGui.BeginCard();
            var enabled = cfg.Enabled;
            if (FlatGui.Toggle(Lang.S.ShowViewer, ref enabled))
            {
                SlimeMod.SetEnabled(enabled);
                dirty = true;
            }
            FlatGui.EndCard();

            return dirty;
        }

        private static void Capture(SlimeConfig cfg)
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None) return;

            e.Use();

            if (e.keyCode == KeyCode.Escape)
            {
                _capturing = false;
                return;
            }

            if (e.keyCode >= KeyCode.Mouse0)
            {
                SlimeLog.Warn("mouse and controller buttons are not supported as key boxes");
                return;
            }

            if (cfg.HasKey(e.keyCode))
            {
                SlimeLog.Warn(KeyNames.Of(e.keyCode) + " already has a box");
                return;
            }

            _pendingAdd = e.keyCode;
            _hasPendingAdd = true;
        }

        private static void Swap(IList<KeyStyle> list, int a, int b)
        {
            var t = list[a];
            list[a] = list[b];
            list[b] = t;
        }
    }
}
