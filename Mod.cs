using System;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SlimeKeyViewer.SlimeMelonMod), "SlimeKeyViewer", "1.0.0", "Hemi")]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]

namespace SlimeKeyViewer
{
    internal static class SlimeLog
    {
        private static MelonLogger.Instance _logger;

        public static void Bind(MelonLogger.Instance logger) => _logger = logger;
        public static void Unbind() => _logger = null;

        public static void Info(string message) => _logger?.Msg(message);
        public static void Warn(string message) => _logger?.Warning(message);

        public static void Error(string message, Exception ex = null)
        {
            if (_logger == null) return;
            if (ex == null) _logger.Error(message);
            else _logger.Error(message + ": " + ex);
        }
    }

    internal static class SlimeMod
    {
        private static SlimeView _view;
        private static bool _initialised;

        public static void Initialize(MelonLogger.Instance logger)
        {
            if (_initialised)
            {
                SlimeLog.Warn("Initialize called twice; ignoring the second call");
                return;
            }
            _initialised = true;

            SlimeLog.Bind(logger);
            SlimeLog.Info("SlimeKeyViewer starting");

            ConfigStore.Load();

            if (ConfigStore.Current.Enabled) SetEnabled(true);
            else SlimeLog.Info("disabled in config; viewer not created");
        }

        public static void SetEnabled(bool enabled)
        {
            ConfigStore.Current.Enabled = enabled;

            if (!enabled)
            {
                DestroyView();
                return;
            }

            if (_view != null) return;

            try
            {
                if (!SlimeAssets.Load()) return;
                _view = SlimeView.Create();
            }
            catch (Exception ex)
            {
                SlimeLog.Error("failed to create the viewer", ex);
                DestroyView();
            }
        }

        private static void DestroyView()
        {
            if (_view == null) return;
            try { UnityEngine.Object.Destroy(_view.gameObject); }
            catch (Exception ex) { SlimeLog.Error("failed to destroy the viewer", ex); }
            _view = null;
        }

        public static void Shutdown()
        {
            if (!_initialised) return;

            ConfigStore.Flush();
            DestroyView();
            Lang.Reset();
            SlimeAssets.Unload();
            RoundedBox.Release();
            StarSprite.Release();
            FontProvider.Reset();
            FlatGui.Release();
            ConfigStore.Unbind();

            SlimeLog.Info("SlimeKeyViewer stopped");
            SlimeLog.Unbind();

            _initialised = false;
        }
    }

    public sealed class SlimeMelonMod : MelonMod
    {
        private const float TitleRowHeight = 40f;

        private static readonly int WindowId = "SlimeKeyViewer".GetHashCode();

        private bool _settingsOpen;
        private bool _placed;
        private int _tab;
        private Rect _window;
        private Vector2 _scroll;

        public override void OnInitializeMelon()
        {
            SlimeMod.Initialize(LoggerInstance);
        }

        public override void OnUpdate()
        {
            ConfigStore.Tick();

            if (!UnityEngine.Input.GetKeyDown(ConfigStore.Current.SettingsKey)) return;

            _settingsOpen = !_settingsOpen;

            if (_settingsOpen) Lang.Refresh();
        }

        public override void OnGUI()
        {
            if (!_settingsOpen) return;

            FlatGui.EnsureStyles();

            float screenW = Screen.width, screenH = Screen.height;
            var height = Mathf.Min(660f, screenH * 0.86f);

            if (!_placed)
            {
                _placed = true;
                _window = new Rect(
                    Mathf.Round((screenW - FlatGui.WindowWidth) * 0.5f),
                    Mathf.Round((screenH - height) * 0.5f),
                    FlatGui.WindowWidth, height);
            }
            _window.width = FlatGui.WindowWidth;
            _window.height = height;
            _window.x = Mathf.Clamp(_window.x, -FlatGui.WindowWidth + 80f, screenW - 80f);
            _window.y = Mathf.Clamp(_window.y, 0f, screenH - 60f);

            _window = GUI.Window(WindowId, _window, DrawWindow, GUIContent.none, FlatGui.Window);
        }

        private void DrawWindow(int id)
        {
            var w = _window.width;
            var h = _window.height;
            var pad = FlatGui.Padding;

            GUI.Label(new Rect(18f, 0f, 200f, TitleRowHeight), "SlimeKeyViewer", FlatGui.Title);

            if (GUI.Button(new Rect(w - 18f - 26f, (TitleRowHeight - 26f) * 0.5f, 26f, 26f), "✕", FlatGui.Button2))
                _settingsOpen = false;

            _tab = FlatGui.TabRailControl(
                new Rect(0f, TitleRowHeight + 4f, w, FlatGui.TabHeight),
                SettingsPage.Tabs, _tab);

            FlatGui.FullDivider(new Rect(0f, FlatGui.HeaderHeight - 1f, w, 1f));

            var body = new Rect(pad, FlatGui.HeaderHeight + 6f, w - pad * 2f, h - FlatGui.HeaderHeight - 6f - pad);
            GUILayout.BeginArea(body);

            _scroll = GUILayout.BeginScrollView(
                _scroll, false, false,
                GUIStyle.none, GUIStyle.none, GUIStyle.none,
                GUILayout.Width(body.width), GUILayout.Height(body.height));

            SettingsPage.Draw(_tab);

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.DragWindow(new Rect(0f, 0f, w - 60f, TitleRowHeight));
        }

        public override void OnDeinitializeMelon()
        {
            SlimeMod.Shutdown();
        }
    }
}
