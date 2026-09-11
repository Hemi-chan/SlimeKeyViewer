using System;
using System.Reflection;
using UnityEngine;

namespace SlimeKeyViewer
{
    internal sealed class LangStrings
    {
        public string TabKeys;
        public string TabBehaviour;

        public string KeysHeading;
        public string Delete;
        public string Edit;
        public string AddKey;
        public string EditAllKeys;
        public string Cancel;
        public string CaptureHint;

        public string Back;
        public string AllKeys;
        public string OneKey;

        public string GroupBox;
        public string GroupSlime;
        public string GroupLabelShadow;
        public string GroupJump;
        public string GroupParticle;
        public string GroupColorsIdle;
        public string GroupColorsPressed;
        public string GroupVisibility;
        public string GroupPlacement;
        public string GroupDisplay;

        public string Width;
        public string Height;
        public string GapToNext;
        public string CornerRadius;
        public string BorderWidth;
        public string FontSize;

        public string Size;
        public string GapFromBox;
        public string Shadow;
        public string OffsetX;
        public string OffsetY;
        public string Opacity;

        public string Distance;
        public string Softness;

        public string JumpOnPress;
        public string JumpHeight;
        public string StepDuration;
        public string FlipEachJump;
        public string FlipOnLanding;

        public string VisibleOnlyOnInput;
        public string FadeDuration;

        public string ParticleAtFeet;

        public string ColorBorder;
        public string ColorFill;
        public string ColorText;

        public string PosX;
        public string PosY;
        public string Anchor;
        public string OverallSize;

        public string ShowViewer;
    }

    internal static class Lang
    {
        public static LangStrings S { get; private set; } = EN.Build();

        private static FieldInfo _languageField;
        private static bool _resolved;
        private static SystemLanguage _applied = (SystemLanguage)(-1);

        public static void Refresh()
        {
            var language = GameLanguage();
            if (language == _applied) return;
            _applied = language;

            var korean = language == SystemLanguage.Korean;
            S = korean ? KR.Build() : EN.Build();
            SlimeLog.Info("interface language: " + (korean ? "Korean" : "English") +
                          " (game reports " + language + ")");
        }

        private static SystemLanguage GameLanguage()
        {
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    var type = Type.GetType("RDString, Assembly-CSharp", false);
                    if (type != null)
                        _languageField = type.GetField("language", BindingFlags.Public | BindingFlags.Static);
                    if (_languageField == null)
                        SlimeLog.Warn("RDString.language not found; falling back to the system language");
                }

                if (_languageField != null) return (SystemLanguage)_languageField.GetValue(null);
            }
            catch (Exception ex)
            {
                SlimeLog.Warn("could not read the game's language: " + ex.Message);
            }

            return Application.systemLanguage;
        }

        public static void Reset()
        {
            _languageField = null;
            _resolved = false;
            _applied = (SystemLanguage)(-1);
        }
    }
}
