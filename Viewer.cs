using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimeKeyViewer
{
    internal static class KeyNames
    {
        private static readonly Dictionary<KeyCode, string> Table = new Dictionary<KeyCode, string>
        {
            { KeyCode.Backspace, "←" },
            { KeyCode.Delete, "Del" },
            { KeyCode.Tab, "Tab" },
            { KeyCode.Return, "↲" },
            { KeyCode.Escape, "Esc" },
            { KeyCode.Space, "Space" },
            { KeyCode.Keypad0, "Keypad 0"},
            { KeyCode.Keypad1, "Keypad 1"},
            { KeyCode.Keypad2, "Keypad 2"},
            { KeyCode.Keypad3, "Keypad 3"},
            { KeyCode.Keypad4, "Keypad 4"},
            { KeyCode.Keypad5, "Keypad 5"},
            { KeyCode.Keypad6, "Keypad 6"},
            { KeyCode.Keypad7, "Keypad 7"},
            { KeyCode.Keypad8, "Keypad 8"},
            { KeyCode.Keypad9, "Keypad 9"},
            { KeyCode.KeypadPeriod, "Keypad ." },
            { KeyCode.KeypadDivide, "Keypad /" },
            { KeyCode.KeypadMultiply, "Keypad *" },
            { KeyCode.KeypadMinus, "Keypad -" },
            { KeyCode.KeypadPlus, "Keypad +" },
            { KeyCode.KeypadEnter, "Keypad ↲" },
            { KeyCode.KeypadEquals, "Keypad =" },
            { KeyCode.UpArrow, "↑" },
            { KeyCode.DownArrow, "↓" },
            { KeyCode.LeftArrow, "←" },
            { KeyCode.RightArrow, "→" },
            { KeyCode.Insert, "Ins" },
            { KeyCode.PageDown, "PgDn" },
            { KeyCode.PageUp, "PgUp" },
            { KeyCode.Alpha0, "0" },
            { KeyCode.Alpha1, "1" },
            { KeyCode.Alpha2, "2" },
            { KeyCode.Alpha3, "3" },
            { KeyCode.Alpha4, "4" },
            { KeyCode.Alpha5, "5" },
            { KeyCode.Alpha6, "6" },
            { KeyCode.Alpha7, "7" },
            { KeyCode.Alpha8, "8" },
            { KeyCode.Alpha9, "9" },
            { KeyCode.Plus, "+"},
            { KeyCode.Minus, "-"},
            { KeyCode.Equals, "="},
            { KeyCode.BackQuote, "`"},
            { KeyCode.LeftBracket, "["},
            { KeyCode.RightBracket, "]"},
            { KeyCode.Backslash, "\\"},
            { KeyCode.Semicolon, ";"},
            { KeyCode.Quote, "'"},
            { KeyCode.Comma, ","},
            { KeyCode.Period, "."},
            { KeyCode.Slash, "/"}
        };

        public static string Of(KeyCode key)
        {
            string name;
            return Table.TryGetValue(key, out name) ? name : key.ToString();
        }
    }

    internal sealed class KeyWatcher
    {
        private readonly HashSet<KeyCode> _held = new HashSet<KeyCode>();
        private readonly List<KeyCode> _sweep = new List<KeyCode>(8);
        private KeyCode[] _watched = new KeyCode[0];

        public event Action<KeyCode> KeyDown;
        public event Action<KeyCode> KeyUp;

        public void SetWatchedKeys(IList<KeyCode> keys)
        {
            var next = new KeyCode[keys.Count];
            for (var i = 0; i < keys.Count; i++) next[i] = keys[i];
            _watched = next;

            if (_held.Count == 0) return;
            _sweep.Clear();
            _sweep.AddRange(_held);
            for (var i = 0; i < _sweep.Count; i++)
                if (Array.IndexOf(_watched, _sweep[i]) < 0) _held.Remove(_sweep[i]);
        }

        public void Poll()
        {
            var keys = _watched;

            for (var i = 0; i < keys.Length; i++)
            {
                var k = keys[i];

                if (UnityEngine.Input.GetKeyDown(k) && _held.Add(k)) KeyDown?.Invoke(k);
            }

            if (_held.Count == 0) return;

            _sweep.Clear();
            _sweep.AddRange(_held);
            for (var i = 0; i < _sweep.Count; i++)
            {
                var k = _sweep[i];

                if (!UnityEngine.Input.GetKey(k) && _held.Remove(k)) KeyUp?.Invoke(k);
            }
        }

        public void ForceReleaseAll()
        {
            _held.Clear();
        }
    }

    internal sealed class SlimeAnimator
    {
        public enum State { Idle, Prep, Jump }

        private const float PrepScaleY = 0.80f;
        private const float ApexScaleY = 1.10f;
        private const float LandScaleY = 0.80f;

        private const float OutBackOvershoot = 3.0f;

        private const float JumpStartMaxScaleY = 0.85f;

        private readonly RectTransform _anchor;
        private readonly Image _image;

        private readonly DOGetter<Vector2> _getAnchorPos;
        private readonly DOSetter<Vector2> _setAnchorPos;

        private Sequence _prepSeq;
        private Sequence _jumpSeq;
        private bool _mirrored;

        private bool _prepSettled;

        public Shadow ImageShadow;

        public Vector2 ShadowOffset;

        public State Current { get; private set; } = State.Idle;

        public Vector2 FootPosition => _anchor.anchoredPosition;

        public float StepDuration = 0.40f;
        public float JumpRise = 300f;
        public bool FlipOnPrep = true;
        public bool FlipOnAutoReturn = false;

        public event Action JumpCompleted;

        public SlimeAnimator(RectTransform anchor, Image image)
        {
            _anchor = anchor;
            _image = image;
            _getAnchorPos = () => _anchor.anchoredPosition;
            _setAnchorPos = v => _anchor.anchoredPosition = v;
            SnapToIdle();
        }

        public void EnterPrep(bool fromKeyPress)
        {
            KillAll();

            if (FlipOnPrep && !FlipOnAutoReturn && fromKeyPress) SetMirrored(!_mirrored);

            SetSprite(SlimeAssets.Prep);
            Current = State.Prep;
            _prepSettled = false;

            var d = StepDuration;
            _prepSeq = DOTween.Sequence();
            _prepSeq.Append(_anchor.DOScaleY(PrepScaleY, d).SetEase(Ease.OutCubic));

            _prepSeq.Join(TweenY(0f, d, Ease.OutCubic));
            _prepSeq.SetUpdate(true);
            _prepSeq.SetRecyclable(false);
            _prepSeq.OnComplete(() => { _prepSeq = null; _prepSettled = true; });
        }

        public void EnterJump(bool flipFirst)
        {
            KillAll();

            if (flipFirst && FlipOnPrep && !FlipOnAutoReturn &&
                Mathf.Abs(_anchor.anchoredPosition.y) <= 1f)
                SetMirrored(!_mirrored);

            var scale = _anchor.localScale;
            if (scale.y > JumpStartMaxScaleY)
            {
                scale.y = JumpStartMaxScaleY;
                _anchor.localScale = scale;
            }

            SetSprite(SlimeAssets.Jump);
            Current = State.Jump;

            var d = StepDuration;
            _jumpSeq = DOTween.Sequence();

            _jumpSeq.Append(_anchor.DOScaleY(ApexScaleY, d).SetEase(Ease.OutCubic));
            _jumpSeq.Join(TweenY(JumpRise, d, Ease.OutCubic));

            _jumpSeq.Append(_anchor.DOScaleY(LandScaleY, d).SetEase(Ease.InCubic));
            _jumpSeq.Join(TweenY(0f, d, Ease.InCubic));

            _jumpSeq.AppendCallback(() => SetSprite(SlimeAssets.Prep));
            _jumpSeq.Append(_anchor.DOScaleY(1f, d).SetEase(Ease.OutBack, OutBackOvershoot));

            _jumpSeq.SetUpdate(true);

            _jumpSeq.SetRecyclable(false);
            _jumpSeq.OnComplete(OnJumpDone);
        }

        private void OnJumpDone()
        {
            _jumpSeq = null;
            SnapToIdle();
            Current = State.Idle;
            JumpCompleted?.Invoke();
        }

        public void ForceReset()
        {
            KillAll();
            SetSprite(SlimeAssets.Prep);
            SnapToIdle();
            Current = State.Idle;
            _prepSettled = false;
        }

        public bool NeedsRecovery
        {
            get
            {
                switch (Current)
                {
                    case State.Prep: return !_prepSettled && !IsAlive(_prepSeq);
                    case State.Jump: return !IsAlive(_jumpSeq);
                    default: return false;
                }
            }
        }

        private static bool IsAlive(Sequence seq) => seq != null && seq.IsActive();

        private void SnapToIdle()
        {
            _anchor.anchoredPosition = Vector2.zero;
            _anchor.localScale = new Vector3(_mirrored ? -1f : 1f, 1f, 1f);
        }

        private void SetMirrored(bool value)
        {
            _mirrored = value;

            var s = _anchor.localScale;
            s.x = value ? -1f : 1f;
            _anchor.localScale = s;

            ApplyShadowOffset();
        }

        public void FlipOnLanding()
        {
            if (!FlipOnPrep || !FlipOnAutoReturn) return;

            if (Mathf.Abs(_anchor.anchoredPosition.y) > 1f) return;
            SetMirrored(!_mirrored);
        }

        public void ApplyShadowOffset()
        {
            if (ImageShadow == null) return;
            ImageShadow.effectDistance = new Vector2(_mirrored ? -ShadowOffset.x : ShadowOffset.x, ShadowOffset.y);
        }

        private void SetSprite(SlimeSprite s)
        {
            if (s == null || _image == null) return;
            if (ReferenceEquals(_image.sprite, s.Sprite)) return;

            _image.sprite = s.Sprite;
            var rt = (RectTransform)_image.transform;
            rt.sizeDelta = s.Size;
            rt.anchoredPosition = s.Offset;
        }

        private Tween TweenY(float to, float duration, Ease ease)
        {
            return DOTween.To(_getAnchorPos, _setAnchorPos, new Vector2(0f, to), duration).SetEase(ease);
        }

        public void KillAll()
        {
            if (IsAlive(_prepSeq)) _prepSeq.Kill(false);
            if (IsAlive(_jumpSeq)) _jumpSeq.Kill(false);
            _prepSeq = null;
            _jumpSeq = null;
        }
    }

    internal sealed class StarBurst
    {
        private const int Count = 10;
        private const int Warm = Count * 3;
        private const int MaxStars = Count * 10;

        private const float Life = 0.5f;
        private const float FadeAt = 0.355886f;
        private const float SpinRate = 360f;
        private const float Damping = 1.8f;
        private const float Gravity = 1400f;
        private const float FootSpread = 90f;

        private static readonly Color WarmEnd = new Color(0.969f, 0.310f, 0.310f, 1f);
        private static readonly Color CoolEnd = new Color(0f, 0.518f, 1f, 1f);

        private readonly RectTransform _root;
        private readonly Sprite _sprite;

        private readonly RectTransform[] _rect = new RectTransform[MaxStars];
        private readonly Image[] _image = new Image[MaxStars];
        private readonly Vector2[] _pos = new Vector2[MaxStars];
        private readonly Vector2[] _vel = new Vector2[MaxStars];
        private readonly float[] _angle = new float[MaxStars];
        private readonly Color[] _tint = new Color[MaxStars];
        private readonly float[] _age = new float[MaxStars];

        private int _created;
        private int _live;

        public StarBurst(RectTransform parent)
        {
            var go = new GameObject("Stars", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            _root.anchoredPosition = Vector2.zero;

            _root.SetAsFirstSibling();

            _sprite = StarSprite.Get();

            for (var i = 0; i < MaxStars; i++) _age[i] = Life;
            while (_created < Warm) Create();

            _root.gameObject.SetActive(false);
        }

        private int Create()
        {
            var i = _created++;

            var star = new GameObject("Star", typeof(RectTransform));
            star.SetActive(false);
            star.transform.SetParent(_root, false);
            _rect[i] = (RectTransform)star.transform;
            _rect[i].anchorMin = _rect[i].anchorMax = new Vector2(0.5f, 0.5f);
            _rect[i].pivot = new Vector2(0.5f, 0.5f);
            _image[i] = star.AddComponent<Image>();
            _image[i].sprite = _sprite;
            _image[i].raycastTarget = false;

            return i;
        }

        private int Take()
        {
            for (var i = 0; i < _created; i++)
                if (_age[i] >= Life) return i;

            if (_created < MaxStars) return Create();

            var oldest = 0;
            for (var i = 1; i < _created; i++)
                if (_age[i] > _age[oldest]) oldest = i;

            return oldest;
        }

        public void Emit(Vector2 origin)
        {
            _root.gameObject.SetActive(true);

            for (var n = 0; n < Count; n++)
            {
                var i = Take();
                if (_age[i] >= Life) _live++;

                var side = (n & 1) == 0 ? 1f : -1f;
                var tilt = UnityEngine.Random.Range(3f, 42f) * Mathf.Deg2Rad;
                var speed = UnityEngine.Random.Range(260f, 460f);

                _pos[i] = origin + new Vector2(UnityEngine.Random.Range(-FootSpread, FootSpread),
                                               UnityEngine.Random.Range(-6f, 6f));

                _vel[i] = new Vector2(side * Mathf.Cos(tilt), -Mathf.Sin(tilt)) * speed;

                _angle[i] = 0f;
                _age[i] = 0f;

                _tint[i] = Color.Lerp(CoolEnd, WarmEnd, UnityEngine.Random.value);

                var size = UnityEngine.Random.Range(110f, 200f);
                _rect[i].sizeDelta = new Vector2(size, size);
                _rect[i].anchoredPosition = _pos[i];
                _rect[i].localRotation = Quaternion.identity;
                _image[i].color = Color.white;
                _rect[i].gameObject.SetActive(true);
            }
        }

        public void Tick(float dt)
        {
            if (_live == 0) return;

            var decay = Mathf.Exp(-Damping * dt);

            for (var i = 0; i < _created; i++)
            {
                if (_age[i] >= Life) continue;

                _age[i] += dt;
                if (_age[i] >= Life)
                {
                    _rect[i].gameObject.SetActive(false);
                    _live--;
                    continue;
                }

                _vel[i] *= decay;
                _vel[i].y -= Gravity * dt;
                _pos[i] += _vel[i] * dt;
                _angle[i] += SpinRate * dt;

                _rect[i].anchoredPosition = _pos[i];
                _rect[i].localRotation = Quaternion.Euler(0f, 0f, _angle[i]);

                var u = _age[i] / Life;

                var c = Color.Lerp(Color.white, _tint[i], u);
                c.a = u < FadeAt ? 1f : 1f - (u - FadeAt) / (1f - FadeAt);
                _image[i].color = c;
            }

            if (_live == 0) _root.gameObject.SetActive(false);
        }

        public void Clear()
        {
            for (var i = 0; i < _created; i++)
            {
                _age[i] = Life;
                _rect[i].gameObject.SetActive(false);
            }

            _live = 0;
            _root.gameObject.SetActive(false);
        }
    }

    internal sealed class KeySlot
    {
        private const float PressedScale = 0.90f;
        private const float PressSpeed = 14f;

        public KeyCode Key { get; private set; }

        private readonly RectTransform _slot;
        private readonly RectTransform _box;
        private readonly RectTransform _fillRect;
        private readonly Image _border;
        private readonly Image _fill;
        private readonly TextMeshProUGUI _label;
        private readonly RectTransform _scaler;
        private readonly RectTransform _anchor;
        private readonly Image _sprite;
        private readonly Shadow _shadow;
        private readonly CanvasGroup _group;
        private readonly StarBurst _stars;
        private readonly SlimeAnimator _anim;

        private Material _labelMaterial;
        private JumpTrigger _trigger = JumpTrigger.OnRelease;
        private bool _pressed;
        private float _scaleFrom = 1f;
        private float _scaleElapsed;
        private float _scaleCurrent = 1f;

        private bool _labelShadow;
        private float _labelShadowDistance = -1f, _labelShadowSoftness = -1f;
        private bool _visibleOnlyOnInput;
        private float _fadeDuration = 0.15f;
        private bool _particleAtFeet = true;

        private Color32 _borderIdle, _fillIdle, _textIdle;
        private Color32 _borderPressed, _fillPressed, _textPressed;

        private float _fade = 1f;

        public KeySlot(RectTransform parent, KeyCode key)
        {
            Key = key;

            _slot = NewRect("Slot", parent);
            _slot.anchorMin = _slot.anchorMax = new Vector2(0.5f, 0.5f);
            _slot.pivot = new Vector2(0.5f, 0.5f);

            _box = NewRect("Box", _slot);
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0.5f);
            _box.pivot = new Vector2(0.5f, 0.5f);

            _fillRect = NewRect("Fill", _box);
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = Vector2.one;
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;
            _fillRect.pivot = new Vector2(0.5f, 0.5f);
            _fill = _fillRect.gameObject.AddComponent<Image>();
            _fill.type = Image.Type.Sliced;
            _fill.raycastTarget = false;

            var borderRect = NewRect("Border", _box);
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            _border = borderRect.gameObject.AddComponent<Image>();
            _border.type = Image.Type.Sliced;
            _border.raycastTarget = false;

            var labelRect = NewRect("Label", _box);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 3f);
            labelRect.offsetMax = new Vector2(-4f, -3f);
            _label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.raycastTarget = false;
            _label.fontStyle = FontStyles.Bold;

            _label.enableAutoSizing = true;

            var font = FontProvider.Resolve();
            if (font != null) _label.font = font;
            _labelMaterial = _label.fontMaterial;

            _label.text = KeyNames.Of(key);

            _scaler = NewRect("Scaler", _slot);
            _scaler.anchorMin = _scaler.anchorMax = new Vector2(0.5f, 0.5f);
            _scaler.pivot = new Vector2(0.5f, 0.5f);

            _group = _scaler.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _stars = new StarBurst(_scaler);

            _anchor = NewRect("Anchor", _scaler);
            _anchor.anchorMin = _anchor.anchorMax = new Vector2(0.5f, 0.5f);
            _anchor.pivot = new Vector2(0.5f, 0.5f);

            var spriteRect = NewRect("Sprite", _anchor);
            spriteRect.anchorMin = spriteRect.anchorMax = new Vector2(0.5f, 0.5f);
            spriteRect.pivot = new Vector2(0.5f, 0.5f);
            _sprite = spriteRect.gameObject.AddComponent<Image>();
            _sprite.raycastTarget = false;
            _sprite.sprite = SlimeAssets.Prep.Sprite;
            spriteRect.sizeDelta = SlimeAssets.Prep.Size;
            spriteRect.anchoredPosition = SlimeAssets.Prep.Offset;

            _shadow = spriteRect.gameObject.AddComponent<Shadow>();
            _shadow.enabled = false;

            _anim = new SlimeAnimator(_anchor, _sprite) { ImageShadow = _shadow };
            _anim.JumpCompleted += OnJumpCompleted;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        public void Apply(SlimeConfig cfg, KeyStyle style, Vector2 position)
        {
            _slot.anchoredPosition = position;

            var w = style.BoxWidth;
            var h = style.BoxHeight;
            _box.sizeDelta = new Vector2(w, h);

            var inset = Mathf.Clamp(style.BoxBorderWidth, 0f, Mathf.Min(w, h) * 0.5f);
            var outerRadius = Mathf.RoundToInt(style.BoxCornerRadius);

            _border.sprite = RoundedBox.Ring(outerRadius, Mathf.Max(1, Mathf.RoundToInt(inset)));
            _border.enabled = inset > 0f;

            _fill.sprite = RoundedBox.Fill(outerRadius);

            _label.fontSizeMax = style.BoxFontSize;
            _label.fontSizeMin = Mathf.Max(6f, style.BoxFontSize * 0.42f);
            ApplyLabelMaterial(style);

            _trigger = cfg.JumpTrigger;
            _visibleOnlyOnInput = cfg.SlimeVisibleOnlyOnInput;
            _fadeDuration = cfg.SlimeFadeDuration;
            _particleAtFeet = cfg.ParticleAtFeet;

            _borderIdle = style.BorderIdle;
            _fillIdle = style.FillIdle;
            _textIdle = style.TextIdle;
            _borderPressed = style.BorderPressed;
            _fillPressed = style.FillPressed;
            _textPressed = style.TextPressed;

            _scaler.anchoredPosition = new Vector2(0f, h * 0.5f + style.SlimeGap);
            _scaler.localScale = new Vector3(style.SlimeScale, style.SlimeScale, 1f);

            _anim.StepDuration = cfg.StepDuration;
            _anim.JumpRise = cfg.JumpRise;
            _anim.FlipOnPrep = cfg.FlipOnPrep;
            _anim.FlipOnAutoReturn = cfg.FlipOnAutoReturn;

            _shadow.enabled = style.SlimeShadow;
            _shadow.effectColor = new Color(0f, 0f, 0f, style.SlimeShadowAlpha);
            _anim.ShadowOffset = new Vector2(style.SlimeShadowX, style.SlimeShadowY);
            _anim.ApplyShadowOffset();

            ApplyColors();

            _border.SetAllDirty();
            _fill.SetAllDirty();
            _label.SetAllDirty();
        }

        private void ApplyLabelMaterial(KeyStyle style)
        {
            if (_labelMaterial == null) return;

            if (_labelShadow == style.LabelShadow &&
                Mathf.Approximately(_labelShadowDistance, style.LabelShadowDistance) &&
                Mathf.Approximately(_labelShadowSoftness, style.LabelShadowSoftness)) return;

            _labelShadow = style.LabelShadow;
            _labelShadowDistance = style.LabelShadowDistance;
            _labelShadowSoftness = style.LabelShadowSoftness;

            _labelMaterial.DisableKeyword("OUTLINE_ON");
            _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);

            if (style.LabelShadow)
            {
                _labelMaterial.EnableKeyword("UNDERLAY_ON");
                _labelMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, style.LabelShadowDistance);
                _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -style.LabelShadowDistance);
                _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, style.LabelShadowSoftness);
            }
            else
            {
                _labelMaterial.DisableKeyword("UNDERLAY_ON");
            }

            _label.UpdateMeshPadding();
        }

        private void ApplyColors()
        {
            _border.color = _pressed ? _borderPressed : _borderIdle;
            _fill.color = _pressed ? _fillPressed : _fillIdle;
            _label.color = _pressed ? _textPressed : _textIdle;
        }

        public void OnKeyDown()
        {
            SetPressed(true);

            if (_trigger == JumpTrigger.OnPress)
            {
                _anim.EnterJump(flipFirst: true);
                _stars.Emit(BurstOrigin);
            }
            else if (_anim.Current != SlimeAnimator.State.Prep) _anim.EnterPrep(true);
        }

        public void OnKeyUp()
        {
            SetPressed(false);

            if (_trigger == JumpTrigger.OnRelease)
            {
                _anim.EnterJump(flipFirst: false);
                _stars.Emit(BurstOrigin);
            }
        }

        private Vector2 BurstOrigin => _particleAtFeet ? _anim.FootPosition : Vector2.zero;

        private void SetPressed(bool pressed)
        {
            if (_pressed == pressed) return;
            _pressed = pressed;

            _scaleFrom = _scaleCurrent;
            _scaleElapsed = 0f;
            ApplyColors();
        }

        private void OnJumpCompleted()
        {
            if (_pressed && _trigger == JumpTrigger.OnRelease)
            {
                _anim.EnterPrep(false);
                return;
            }

            _anim.FlipOnLanding();
        }

        private bool WantsVisible =>
            !_visibleOnlyOnInput || _anim.Current != SlimeAnimator.State.Idle;

        private void ApplyFade()
        {
            _group.alpha = _fade * _fade * (3f - 2f * _fade);
        }

        private void SetFadeImmediate(float value)
        {
            _fade = value;
            ApplyFade();
        }

        public void Tick(float unscaledDelta)
        {
            _stars.Tick(unscaledDelta);

            var fadeTarget = WantsVisible ? 1f : 0f;
            if (!Mathf.Approximately(_fade, fadeTarget))
            {
                _fade = Mathf.MoveTowards(_fade, fadeTarget, unscaledDelta / Mathf.Max(0.02f, _fadeDuration));
                ApplyFade();
            }

            var target = _pressed ? PressedScale : 1f;
            if (!Mathf.Approximately(_scaleCurrent, target))
            {
                _scaleElapsed += unscaledDelta;
                var progress = 1f - Mathf.Exp(-PressSpeed * _scaleElapsed);
                if (progress > 0.999f) progress = 1f;

                var eased = Mathf.Sqrt(1f - (progress - 1f) * (progress - 1f));
                _scaleCurrent = Mathf.LerpUnclamped(_scaleFrom, target, eased);
                _box.localScale = new Vector3(_scaleCurrent, _scaleCurrent, 1f);
            }

            if (!_anim.NeedsRecovery) return;

            _anim.ForceReset();
        }

        public void ForceReset()
        {
            SetPressed(false);
            _scaleCurrent = 1f;
            _box.localScale = Vector3.one;
            _anim.ForceReset();
            _stars.Clear();

            SetFadeImmediate(WantsVisible ? 1f : 0f);
        }

        public void Destroy()
        {
            _anim.JumpCompleted -= OnJumpCompleted;
            _anim.KillAll();
            _labelMaterial = null;
            if (_slot != null) UnityEngine.Object.Destroy(_slot.gameObject);
        }
    }

    internal sealed class SlimeView : MonoBehaviour
    {
        private const string RootName = "SlimeKeyViewerRoot";

        private RectTransform _board;
        private KeyWatcher _keys;

        private readonly List<KeySlot> _slots = new List<KeySlot>();
        private readonly Dictionary<KeyCode, KeySlot> _byKey = new Dictionary<KeyCode, KeySlot>();
        private readonly List<KeyCode> _watched = new List<KeyCode>();

        private bool _built;
        private bool _hadFocus = true;

        public static SlimeView Create()
        {
            var go = new GameObject(RootName);
            DontDestroyOnLoad(go);
            return go.AddComponent<SlimeView>();
        }

        private void Awake()
        {
            try { Build(); _built = true; }
            catch (Exception ex) { SlimeLog.Error("view could not be built", ex); }
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 31000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = 1f;

            var boardGo = new GameObject("Board", typeof(RectTransform));
            boardGo.transform.SetParent(transform, false);
            _board = (RectTransform)boardGo.transform;
            _board.sizeDelta = Vector2.zero;
            _board.pivot = new Vector2(0.5f, 0.5f);

            _keys = new KeyWatcher();
            _keys.KeyDown += OnKeyDown;
            _keys.KeyUp += OnKeyUp;

            ApplyConfig();
            ConfigStore.Changed += ApplyConfig;

            SlimeLog.Info("viewer created with " + _slots.Count + " key boxes");
        }

        public void ApplyConfig()
        {
            if (_board == null) return;
            var cfg = ConfigStore.Current;

            _board.anchorMin = _board.anchorMax = new Vector2(cfg.PosX, cfg.PosY);
            _board.anchoredPosition = Vector2.zero;
            _board.localScale = new Vector3(cfg.Size, cfg.Size, 1f);

            SyncSlots(cfg);

            var count = _slots.Count;
            var totalWidth = 0f;
            var maxHeight = 0f;
            for (var i = 0; i < count; i++)
            {
                totalWidth += cfg.Keys[i].BoxWidth;
                if (i < count - 1) totalWidth += cfg.Keys[i].BoxSpacing;
                if (cfg.Keys[i].BoxHeight > maxHeight) maxHeight = cfg.Keys[i].BoxHeight;
            }

            var pivot = SlimeConfig.Pivot(cfg.Anchor);
            var y = maxHeight * (0.5f - pivot.y);
            var x = -totalWidth * pivot.x;
            for (var i = 0; i < count; i++)
            {
                var style = cfg.Keys[i];
                _slots[i].Apply(cfg, style, new Vector2(x + style.BoxWidth * 0.5f, y));
                x += style.BoxWidth + style.BoxSpacing;
            }

            _keys.SetWatchedKeys(_watched);
        }

        private void SyncSlots(SlimeConfig cfg)
        {
            var same = _slots.Count == cfg.Keys.Count;
            if (same)
            {
                for (var i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i].Key == cfg.Keys[i].Key) continue;
                    same = false;
                    break;
                }
            }
            if (same) return;

            foreach (var slot in _slots) slot.Destroy();
            _slots.Clear();
            _byKey.Clear();
            _watched.Clear();

            foreach (var style in cfg.Keys)
            {
                var slot = new KeySlot(_board, style.Key);
                _slots.Add(slot);
                _byKey[style.Key] = slot;
                _watched.Add(style.Key);
            }
        }

        private void OnKeyDown(KeyCode key)
        {
            KeySlot slot;
            if (_byKey.TryGetValue(key, out slot)) slot.OnKeyDown();
        }

        private void OnKeyUp(KeyCode key)
        {
            KeySlot slot;
            if (_byKey.TryGetValue(key, out slot)) slot.OnKeyUp();
        }

        private void Update()
        {
            if (!_built) return;

            var focused = Application.isFocused;
            if (focused != _hadFocus)
            {
                _hadFocus = focused;
                if (!focused) ForceResetAll("focus lost");
            }
            if (!focused) return;

            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _slots.Count; i++) _slots[i].Tick(dt);

            if (IsTypingInUi()) return;

            _keys.Poll();
        }

        private static bool IsTypingInUi()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var sel = es.currentSelectedGameObject;
            if (sel == null) return false;
            if (sel.GetComponent<InputField>() != null) return true;
            if (sel.GetComponent<TMPro.TMP_InputField>() != null) return true;
            return false;
        }

        private void ForceResetAll(string reason)
        {
            _keys.ForceReleaseAll();
            for (var i = 0; i < _slots.Count; i++) _slots[i].ForceReset();
            SlimeLog.Info("reset (" + reason + ")");
        }

        private void OnDisable()
        {
            if (_built) ForceResetAll("disabled");
        }

        private void OnDestroy()
        {
            ConfigStore.Changed -= ApplyConfig;

            foreach (var slot in _slots) slot.Destroy();
            _slots.Clear();
            _byKey.Clear();

            SlimeLog.Info("viewer destroyed");
        }
    }
}
