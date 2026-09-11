using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace SlimeKeyViewer
{
    internal enum ViewerAnchor
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }

    internal enum JumpTrigger
    {
        OnRelease,

        OnPress
    }

    internal sealed class KeyStyle
    {
        public KeyCode Key = KeyCode.None;

        public float BoxWidth = 60f;
        public float BoxHeight = 60f;

        public float BoxSpacing = 8f;

        public float BoxCornerRadius = 10f;
        public float BoxBorderWidth = 3f;
        public float BoxFontSize = 22f;

        public float SlimeScale = 0.20f;

        public float SlimeGap = 4f;

        public bool SlimeShadow = false;
        public float SlimeShadowX = 8f;
        public float SlimeShadowY = -8f;
        public float SlimeShadowAlpha = 0.35f;

        public Color32 BorderIdle = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color32 FillIdle = new Color32(0x00, 0x00, 0x00, 0x80);
        public Color32 TextIdle = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        public Color32 BorderPressed = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color32 FillPressed = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color32 TextPressed = new Color32(0x00, 0x00, 0x00, 0xFF);

        public bool LabelShadow = true;
        public float LabelShadowDistance = 0.35f;
        public float LabelShadowSoftness = 0.05f;

        public KeyStyle Clone() => (KeyStyle)MemberwiseClone();

        public void CopyLookFrom(KeyStyle other)
        {
            var key = Key;
            var copy = other.Clone();
            copy.Key = key;
            BoxWidth = copy.BoxWidth; BoxHeight = copy.BoxHeight; BoxSpacing = copy.BoxSpacing;
            BoxCornerRadius = copy.BoxCornerRadius; BoxBorderWidth = copy.BoxBorderWidth;
            BoxFontSize = copy.BoxFontSize;
            SlimeScale = copy.SlimeScale; SlimeGap = copy.SlimeGap;
            SlimeShadow = copy.SlimeShadow; SlimeShadowX = copy.SlimeShadowX;
            SlimeShadowY = copy.SlimeShadowY; SlimeShadowAlpha = copy.SlimeShadowAlpha;
            LabelShadow = copy.LabelShadow; LabelShadowDistance = copy.LabelShadowDistance;
            LabelShadowSoftness = copy.LabelShadowSoftness;
            CopyColorsFrom(copy);
        }

        public void CopyColorsFrom(KeyStyle other)
        {
            BorderIdle = other.BorderIdle; FillIdle = other.FillIdle; TextIdle = other.TextIdle;
            BorderPressed = other.BorderPressed; FillPressed = other.FillPressed;
            TextPressed = other.TextPressed;
        }

        public void Clamp()
        {
            BoxWidth = Mathf.Clamp(BoxWidth, 16f, 400f);
            BoxHeight = Mathf.Clamp(BoxHeight, 16f, 400f);
            BoxSpacing = Mathf.Clamp(BoxSpacing, 0f, 200f);

            var halfBox = Mathf.Min(BoxWidth, BoxHeight) * 0.5f;
            BoxCornerRadius = Mathf.Clamp(BoxCornerRadius, 0f, Mathf.Max(0f, halfBox - 1f));
            BoxBorderWidth = Mathf.Clamp(BoxBorderWidth, 0f, halfBox);
            BoxFontSize = Mathf.Clamp(BoxFontSize, 6f, 200f);
            SlimeScale = Mathf.Clamp(SlimeScale, 0.02f, 2f);
            SlimeGap = Mathf.Clamp(SlimeGap, -200f, 400f);
            SlimeShadowX = Mathf.Clamp(SlimeShadowX, -100f, 100f);
            SlimeShadowY = Mathf.Clamp(SlimeShadowY, -100f, 100f);
            SlimeShadowAlpha = Mathf.Clamp01(SlimeShadowAlpha);
            LabelShadowDistance = Mathf.Clamp(LabelShadowDistance, 0f, 1f);
            LabelShadowSoftness = Mathf.Clamp01(LabelShadowSoftness);
        }

        public string Serialize()
        {
            var sb = new StringBuilder(160);
            sb.Append("k:").Append(Key);
            Put(sb, "bw", BoxWidth); Put(sb, "bh", BoxHeight); Put(sb, "sp", BoxSpacing);
            Put(sb, "cr", BoxCornerRadius); Put(sb, "bd", BoxBorderWidth); Put(sb, "fs", BoxFontSize);
            Put(sb, "ss", SlimeScale); Put(sb, "sg", SlimeGap);
            sb.Append(";sh:").Append(SlimeShadow ? '1' : '0');
            Put(sb, "sx", SlimeShadowX); Put(sb, "sy", SlimeShadowY); Put(sb, "sa", SlimeShadowAlpha);
            sb.Append(";ls:").Append(LabelShadow ? '1' : '0');
            Put(sb, "ld", LabelShadowDistance); Put(sb, "lf", LabelShadowSoftness);
            PutHex(sb, "bi", BorderIdle); PutHex(sb, "fi", FillIdle); PutHex(sb, "ti", TextIdle);
            PutHex(sb, "bp", BorderPressed); PutHex(sb, "fp", FillPressed); PutHex(sb, "tp", TextPressed);
            return sb.ToString();
        }

        private static void Put(StringBuilder sb, string token, float value)
        {
            sb.Append(';').Append(token).Append(':').Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void PutHex(StringBuilder sb, string token, Color32 value)
        {
            sb.Append(';').Append(token).Append(':').Append(SlimeConfig.ColorToHex(value));
        }

        public static KeyStyle Deserialize(string line, KeyStyle template)
        {
            var style = template.Clone();
            var named = false;

            foreach (var part in line.Split(';'))
            {
                var text = part.Trim();
                if (text.Length == 0) continue;

                var colon = text.IndexOf(':');
                if (colon <= 0) { SlimeLog.Warn("config: skipping key field \"" + text + "\""); continue; }

                var token = text.Substring(0, colon);
                var value = text.Substring(colon + 1);

                switch (token)
                {
                    case "k":
                        var code = SlimeConfig.ParseKeyCode(value);
                        if (code == KeyCode.None || code >= KeyCode.Mouse0)
                        {
                            SlimeLog.Warn("config: \"" + value + "\" is not a usable key; the entry is dropped");
                            return null;
                        }
                        style.Key = code;
                        named = true;
                        break;

                    case "bw": style.BoxWidth = F(token, value, style.BoxWidth); break;
                    case "bh": style.BoxHeight = F(token, value, style.BoxHeight); break;
                    case "sp": style.BoxSpacing = F(token, value, style.BoxSpacing); break;
                    case "cr": style.BoxCornerRadius = F(token, value, style.BoxCornerRadius); break;
                    case "bd": style.BoxBorderWidth = F(token, value, style.BoxBorderWidth); break;
                    case "fs": style.BoxFontSize = F(token, value, style.BoxFontSize); break;
                    case "ss": style.SlimeScale = F(token, value, style.SlimeScale); break;
                    case "sg": style.SlimeGap = F(token, value, style.SlimeGap); break;
                    case "sh": style.SlimeShadow = value == "1"; break;
                    case "sx": style.SlimeShadowX = F(token, value, style.SlimeShadowX); break;
                    case "sy": style.SlimeShadowY = F(token, value, style.SlimeShadowY); break;
                    case "sa": style.SlimeShadowAlpha = F(token, value, style.SlimeShadowAlpha); break;
                    case "ls": style.LabelShadow = value == "1"; break;
                    case "ld": style.LabelShadowDistance = F(token, value, style.LabelShadowDistance); break;
                    case "lf": style.LabelShadowSoftness = F(token, value, style.LabelShadowSoftness); break;
                    case "bi": style.BorderIdle = C(token, value, style.BorderIdle); break;
                    case "fi": style.FillIdle = C(token, value, style.FillIdle); break;
                    case "ti": style.TextIdle = C(token, value, style.TextIdle); break;
                    case "bp": style.BorderPressed = C(token, value, style.BorderPressed); break;
                    case "fp": style.FillPressed = C(token, value, style.FillPressed); break;
                    case "tp": style.TextPressed = C(token, value, style.TextPressed); break;
                    default: SlimeLog.Warn("config: ignoring unknown key field \"" + token + "\""); break;
                }
            }

            if (!named) { SlimeLog.Warn("config: a key entry had no key name; dropped"); return null; }
            style.Clamp();
            return style;
        }

        private static float F(string token, string value, float fallback) =>
            SlimeConfig.ParseFloat(token, value, fallback);

        private static Color32 C(string token, string value, Color32 fallback) =>
            SlimeConfig.ParseColor(token, value, fallback);
    }

    internal sealed class SlimeConfig
    {
        public bool Enabled = true;

        public List<KeyStyle> Keys = new List<KeyStyle>();

        public KeyStyle Template = new KeyStyle();

        public JumpTrigger JumpTrigger = JumpTrigger.OnPress;

        public float PosX = 0.5f;
        public float PosY = 0.5f;
        public float Size = 0.7f;

        public ViewerAnchor Anchor = ViewerAnchor.BottomLeft;

        public float JumpRise = 300f;
        public float StepDuration = 0.10f;

        public bool FlipOnPrep = true;

        public bool FlipOnAutoReturn = false;

        public bool SlimeVisibleOnlyOnInput = true;
        public float SlimeFadeDuration = 0.10f;

        public bool ParticleAtFeet = true;

        public KeyCode SettingsKey = KeyCode.F10;

        public SlimeConfig()
        {
            Keys.Add(NewKey(KeyCode.A));
        }

        public KeyStyle NewKey(KeyCode key)
        {
            var style = Template.Clone();
            style.Key = key;
            return style;
        }

        public void ApplyTemplateToAll()
        {
            foreach (var style in Keys) style.CopyLookFrom(Template);
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("enabled=").Append(Enabled ? "true" : "false").Append('\n');
            sb.Append("jumpTrigger=").Append(JumpTrigger.ToString()).Append('\n');

            sb.Append("template=").Append(Template.Serialize()).Append('\n');
            foreach (var style in Keys) sb.Append("key=").Append(style.Serialize()).Append('\n');

            Put(sb, "posX", PosX);
            Put(sb, "posY", PosY);
            Put(sb, "size", Size);
            sb.Append("anchor=").Append(Anchor.ToString()).Append('\n');
            Put(sb, "jumpRise", JumpRise);
            Put(sb, "stepDuration", StepDuration);
            sb.Append("flipOnPrep=").Append(FlipOnPrep ? "true" : "false").Append('\n');
            sb.Append("flipOnAutoReturn=").Append(FlipOnAutoReturn ? "true" : "false").Append('\n');
            sb.Append("slimeVisibleOnlyOnInput=").Append(SlimeVisibleOnlyOnInput ? "true" : "false").Append('\n');
            Put(sb, "slimeFadeDuration", SlimeFadeDuration);
            sb.Append("particleAtFeet=").Append(ParticleAtFeet ? "true" : "false").Append('\n');
            sb.Append("settingsKey=").Append(SettingsKey.ToString()).Append('\n');
            return sb.ToString();
        }

        private static void Put(StringBuilder sb, string key, float value)
        {
            sb.Append(key).Append('=').Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }


        public static SlimeConfig Deserialize(string text)
        {
            var cfg = new SlimeConfig();
            if (string.IsNullOrEmpty(text)) return cfg;

            var keys = new List<KeyStyle>();
            var sawKeyLine = false;
            var sawTemplate = false;
            List<KeyCode> legacyKeys = null;

            foreach (var raw in text.Replace('\r', '\n').Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var eq = line.IndexOf('=');
                if (eq <= 0) { SlimeLog.Warn("config: skipping unparseable line \"" + line + "\""); continue; }

                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "enabled": cfg.Enabled = ParseBool(val, cfg.Enabled); break;
                    case "jumpTrigger": cfg.JumpTrigger = ParseTrigger(val, cfg.JumpTrigger); break;

                    case "template":
                        sawTemplate = true;
                        var t = KeyStyle.Deserialize("k:A;" + val, cfg.Template);
                        if (t != null) { t.Key = KeyCode.None; cfg.Template = t; }
                        break;

                    case "key":
                        sawKeyLine = true;
                        var style = KeyStyle.Deserialize(val, cfg.Template);
                        if (style != null) keys.Add(style);
                        break;

                    case "posX": cfg.PosX = ParseFloat(key, val, cfg.PosX); break;
                    case "posY": cfg.PosY = ParseFloat(key, val, cfg.PosY); break;
                    case "size": cfg.Size = ParseFloat(key, val, cfg.Size); break;
                    case "anchor": cfg.Anchor = ParseAnchor(val, cfg.Anchor); break;
                    case "jumpRise": cfg.JumpRise = ParseFloat(key, val, cfg.JumpRise); break;
                    case "stepDuration": cfg.StepDuration = ParseFloat(key, val, cfg.StepDuration); break;
                    case "flipOnPrep": cfg.FlipOnPrep = ParseBool(val, cfg.FlipOnPrep); break;
                    case "flipOnAutoReturn": cfg.FlipOnAutoReturn = ParseBool(val, cfg.FlipOnAutoReturn); break;

                    case "slimeVisibleOnlyOnInput":
                        cfg.SlimeVisibleOnlyOnInput = ParseBool(val, cfg.SlimeVisibleOnlyOnInput); break;
                    case "slimeFadeDuration":
                        cfg.SlimeFadeDuration = ParseFloat(key, val, cfg.SlimeFadeDuration); break;

                    case "particleAtFeet":
                        cfg.ParticleAtFeet = ParseBool(val, cfg.ParticleAtFeet); break;

                    case "settingsKey": cfg.SettingsKey = ParseKey(val, cfg.SettingsKey); break;

                    case "keys": legacyKeys = ParseLegacyKeys(val); break;
                    case "boxWidth": cfg.Template.BoxWidth = ParseFloat(key, val, cfg.Template.BoxWidth); break;
                    case "boxHeight": cfg.Template.BoxHeight = ParseFloat(key, val, cfg.Template.BoxHeight); break;
                    case "boxSpacing": cfg.Template.BoxSpacing = ParseFloat(key, val, cfg.Template.BoxSpacing); break;
                    case "boxCornerRadius": cfg.Template.BoxCornerRadius = ParseFloat(key, val, cfg.Template.BoxCornerRadius); break;
                    case "boxBorderWidth": cfg.Template.BoxBorderWidth = ParseFloat(key, val, cfg.Template.BoxBorderWidth); break;
                    case "boxFontSize": cfg.Template.BoxFontSize = ParseFloat(key, val, cfg.Template.BoxFontSize); break;
                    case "slimeScale": cfg.Template.SlimeScale = ParseFloat(key, val, cfg.Template.SlimeScale); break;
                    case "slimeGap": cfg.Template.SlimeGap = ParseFloat(key, val, cfg.Template.SlimeGap); break;
                    case "slimeShadow": cfg.Template.SlimeShadow = ParseBool(val, cfg.Template.SlimeShadow); break;
                    case "slimeShadowX": cfg.Template.SlimeShadowX = ParseFloat(key, val, cfg.Template.SlimeShadowX); break;
                    case "slimeShadowY": cfg.Template.SlimeShadowY = ParseFloat(key, val, cfg.Template.SlimeShadowY); break;
                    case "slimeShadowAlpha": cfg.Template.SlimeShadowAlpha = ParseFloat(key, val, cfg.Template.SlimeShadowAlpha); break;
                    case "labelShadow": cfg.Template.LabelShadow = ParseBool(val, cfg.Template.LabelShadow); break;
                    case "labelShadowDistance": cfg.Template.LabelShadowDistance = ParseFloat(key, val, cfg.Template.LabelShadowDistance); break;
                    case "labelShadowSoftness": cfg.Template.LabelShadowSoftness = ParseFloat(key, val, cfg.Template.LabelShadowSoftness); break;

                    case "labelGap":
                    case "labelSize":
                    case "pivotX":
                    case "pivotY":
                    case "borderIdle":
                    case "fillIdle":
                    case "textIdle":
                    case "borderPressed":
                    case "fillPressed":
                    case "textPressed": break;

                    default: SlimeLog.Warn("config: ignoring unknown key \"" + key + "\""); break;
                }
            }

            if (sawKeyLine)
            {
                if (keys.Count > 0) cfg.Keys = keys;
                else SlimeLog.Warn("config: every key entry was unusable; keeping the defaults");
            }
            else if (sawTemplate)
            {
                cfg.Keys = new List<KeyStyle>();
                SlimeLog.Info("config has no keys; the viewer stays empty until one is added");
            }
            else if (legacyKeys != null && legacyKeys.Count > 0)
            {
                cfg.Keys = new List<KeyStyle>();
                foreach (var k in legacyKeys) cfg.Keys.Add(cfg.NewKey(k));
                SlimeLog.Info("config upgraded: " + legacyKeys.Count + " keys now carry their own style");
            }

            cfg.Clamp();
            return cfg;
        }

        public void Clamp()
        {
            PosX = Mathf.Clamp01(PosX);
            PosY = Mathf.Clamp01(PosY);
            Size = Mathf.Clamp(Size, 0.1f, 3f);
            JumpRise = Mathf.Clamp(JumpRise, 0f, 2000f);
            StepDuration = Mathf.Clamp(StepDuration, 0.05f, 2f);
            SlimeFadeDuration = Mathf.Clamp(SlimeFadeDuration, 0.02f, 1.5f);

            Template.Clamp();
            if (Keys == null) Keys = new List<KeyStyle>();

            for (var i = Keys.Count - 1; i >= 0; i--)
            {
                if (Keys[i] == null) { Keys.RemoveAt(i); continue; }
                Keys[i].Clamp();
                for (var j = 0; j < i; j++)
                {
                    if (Keys[j].Key != Keys[i].Key) continue;
                    Keys.RemoveAt(i);
                    break;
                }
            }
        }

        public bool HasKey(KeyCode key)
        {
            foreach (var style in Keys) if (style.Key == key) return true;
            return false;
        }

        private static bool ParseBool(string v, bool fallback)
        {
            if (v == "true" || v == "True" || v == "1") return true;
            if (v == "false" || v == "False" || v == "0") return false;
            SlimeLog.Warn("config: \"" + v + "\" is not a boolean; keeping " + fallback);
            return fallback;
        }

        public static string ColorToHex(Color32 c)
        {
            return c.r.ToString("X2", CultureInfo.InvariantCulture) +
                   c.g.ToString("X2", CultureInfo.InvariantCulture) +
                   c.b.ToString("X2", CultureInfo.InvariantCulture) +
                   c.a.ToString("X2", CultureInfo.InvariantCulture);
        }

        public static bool TryParseColor(string v, byte alphaIfMissing, out Color32 color)
        {
            color = default(Color32);
            if (v == null) return false;

            var text = v.Trim();
            if (text.Length > 0 && text[0] == '#') text = text.Substring(1);

            uint n;
            if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n)) return false;

            if (text.Length == 8)
            {
                color = new Color32((byte)(n >> 24), (byte)(n >> 16), (byte)(n >> 8), (byte)n);
                return true;
            }
            if (text.Length == 6)
            {
                color = new Color32((byte)(n >> 16), (byte)(n >> 8), (byte)n, alphaIfMissing);
                return true;
            }
            return false;
        }

        public static Color32 ParseColor(string key, string v, Color32 fallback)
        {
            Color32 parsed;
            if (TryParseColor(v, 0xFF, out parsed)) return parsed;

            SlimeLog.Warn("config: " + key + "=\"" + v + "\" is not an RRGGBBAA colour; keeping the default");
            return fallback;
        }

        public static float ParseFloat(string key, string v, float fallback)
        {
            float result;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
                !float.IsNaN(result) && !float.IsInfinity(result))
                return result;

            SlimeLog.Warn("config: " + key + "=\"" + v + "\" is not a number; keeping " + fallback);
            return fallback;
        }

        public static Vector2 Pivot(ViewerAnchor anchor)
        {
            var i = (int)anchor;
            return new Vector2((i % 3) * 0.5f, 1f - (i / 3) * 0.5f);
        }

        private static ViewerAnchor ParseAnchor(string v, ViewerAnchor fallback)
        {
            try
            {
                var parsed = (ViewerAnchor)Enum.Parse(typeof(ViewerAnchor), v, true);
                if (Enum.IsDefined(typeof(ViewerAnchor), parsed)) return parsed;
            }
            catch (Exception) { }

            SlimeLog.Warn("config: \"" + v + "\" is not a viewer anchor; keeping " + fallback);
            return fallback;
        }

        private static JumpTrigger ParseTrigger(string v, JumpTrigger fallback)
        {
            if (v == "OnPress") return JumpTrigger.OnPress;
            if (v == "OnRelease") return JumpTrigger.OnRelease;
            SlimeLog.Warn("config: \"" + v + "\" is not a jump trigger; keeping " + fallback);
            return fallback;
        }

        public static KeyCode ParseKeyCode(string v)
        {
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), v, true); }
            catch (Exception) { return KeyCode.None; }
        }

        private static KeyCode ParseKey(string v, KeyCode fallback)
        {
            var code = ParseKeyCode(v);
            if (code != KeyCode.None) return code;
            SlimeLog.Warn("config: \"" + v + "\" is not a KeyCode; keeping " + fallback);
            return fallback;
        }

        private static List<KeyCode> ParseLegacyKeys(string v)
        {
            var list = new List<KeyCode>();
            foreach (var part in v.Split(','))
            {
                var name = part.Trim();
                if (name.Length == 0) continue;
                var code = ParseKeyCode(name);
                if (code == KeyCode.None || code >= KeyCode.Mouse0)
                {
                    SlimeLog.Warn("config: \"" + name + "\" is not a usable key; dropped");
                    continue;
                }
                if (!list.Contains(code)) list.Add(code);
            }
            return list;
        }
    }

    internal static class ConfigStore
    {
        private const float SaveDelay = 0.4f;

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<string> _entry;
        private static float _saveDueAt = -1f;

        public static SlimeConfig Current { get; private set; } = new SlimeConfig();

        public static event Action Changed;

        public static void Load()
        {
            try
            {
                _category = MelonPreferences.CreateCategory("SlimeKeyViewer");
                _entry = _category.CreateEntry("Config", string.Empty, "SlimeKeyViewer settings");

                var text = _entry.Value;
                if (string.IsNullOrEmpty(text))
                {
                    Current = new SlimeConfig();
                    SlimeLog.Info("no stored config; using defaults");
                }
                else
                {
                    Current = SlimeConfig.Deserialize(text);
                    SlimeLog.Info("config loaded (" + Current.Keys.Count + " keys)");
                }
            }
            catch (Exception ex)
            {
                Current = new SlimeConfig();
                SlimeLog.Error("config could not be read; running on defaults", ex);
            }
        }

        public static void MarkChanged()
        {
            Current.Clamp();
            _saveDueAt = Time.unscaledTime + SaveDelay;

            var handler = Changed;
            if (handler == null) return;
            try { handler(); }
            catch (Exception ex) { SlimeLog.Error("config change handler threw", ex); }
        }

        public static void Tick()
        {
            if (_saveDueAt >= 0f && Time.unscaledTime >= _saveDueAt) Flush();
        }

        public static void Flush()
        {
            if (_saveDueAt < 0f) return;
            _saveDueAt = -1f;
            if (_entry == null) return;

            try
            {
                _entry.Value = Current.Serialize();
                _category.SaveToFile(false);
            }
            catch (Exception ex) { SlimeLog.Error("config could not be saved", ex); }
        }

        public static void Unbind()
        {
            Flush();
            Changed = null;
        }
    }
}
