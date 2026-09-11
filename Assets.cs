using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace SlimeKeyViewer
{
    internal static class ImageDecoder
    {
        private static MethodInfo _loadImage;
        private static bool _resolved;

        private static MethodInfo Resolve()
        {
            if (_resolved) return _loadImage;
            _resolved = true;

            var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", false)
                    ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine", false);

            if (type == null)
            {
                SlimeLog.Error("UnityEngine.ImageConversion type not found — sprites cannot be decoded", null);
                return null;
            }

            _loadImage = type.GetMethod(
                "LoadImage",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(Texture2D), typeof(byte[]) },
                null);

            if (_loadImage == null)
                SlimeLog.Error("ImageConversion.LoadImage(Texture2D, byte[]) not found — sprites cannot be decoded", null);

            return _loadImage;
        }

        public static Texture2D Decode(byte[] png, string label)
        {
            var method = Resolve();
            if (method == null) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                var ok = (bool)method.Invoke(null, new object[] { tex, png });
                if (!ok)
                {
                    SlimeLog.Error("LoadImage rejected the PNG for \"" + label + "\"", null);
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
            }
            catch (Exception ex)
            {
                SlimeLog.Error("LoadImage threw while decoding \"" + label + "\"", ex);
                UnityEngine.Object.Destroy(tex);
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }

    internal sealed class SlimeSprite
    {
        public Sprite Sprite;
        public Vector2 Size;

        public Vector2 Offset;
    }

    internal static class SlimeAssets
    {
        private const float PrepW = 375f, PrepH = 318f, PrepFootX = 187.8f, PrepFootY = 257f;
        private const float JumpW = 433f, JumpH = 298f, JumpFootX = 213.8f, JumpFootY = 248f;

        private static SlimeSprite _prep;
        private static SlimeSprite _jump;

        public static SlimeSprite Prep => _prep;
        public static SlimeSprite Jump => _jump;
        public static bool Ready => _prep != null && _jump != null;

        public static bool Load()
        {
            if (Ready) return true;

            _prep = Build("SlimeKeyViewer.prep.png", PrepW, PrepH, PrepFootX, PrepFootY);
            _jump = Build("SlimeKeyViewer.jump.png", JumpW, JumpH, JumpFootX, JumpFootY);

            if (!Ready)
            {
                SlimeLog.Error("sprites failed to load — the viewer will not be created", null);
                Unload();
                return false;
            }

            SlimeLog.Info("sprites loaded: prep " + Prep.Size.x + "x" + Prep.Size.y +
                          " offset " + Prep.Offset + ", jump " + Jump.Size.x + "x" + Jump.Size.y +
                          " offset " + Jump.Offset);
            return true;
        }

        private static SlimeSprite Build(string resource, float w, float h, float footX, float footY)
        {
            var bytes = ReadResource(resource);
            if (bytes == null) return null;

            var tex = ImageDecoder.Decode(bytes, resource);
            if (tex == null) return null;

            if (tex.width != (int)w || tex.height != (int)h)
            {
                SlimeLog.Warn(resource + " is " + tex.width + "x" + tex.height +
                              " but the pivot data was measured on " + (int)w + "x" + (int)h +
                              "; the ground contact point will be off");
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.name = resource;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            var offset = new Vector2(tex.width * 0.5f - footX, footY - tex.height * 0.5f);

            return new SlimeSprite
            {
                Sprite = sprite,
                Size = new Vector2(tex.width, tex.height),
                Offset = offset
            };
        }

        private static byte[] ReadResource(string name)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(name))
                {
                    if (stream == null)
                    {
                        SlimeLog.Error("embedded resource \"" + name + "\" not found in the assembly. " +
                                       "Available: " + string.Join(", ", asm.GetManifestResourceNames()), null);
                        return null;
                    }
                    return new BinaryReader(stream).ReadBytes((int)stream.Length);
                }
            }
            catch (Exception ex)
            {
                SlimeLog.Error("could not read embedded resource \"" + name + "\"", ex);
                return null;
            }
        }

        public static void Unload()
        {
            Release(ref _prep);
            Release(ref _jump);
        }

        private static void Release(ref SlimeSprite slot)
        {
            if (slot == null) return;
            var sprite = slot.Sprite;
            slot = null;
            if (sprite == null) return;

            var tex = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (tex != null) UnityEngine.Object.Destroy(tex);
        }
    }

    internal static class FontProvider
    {
        private static TMP_FontAsset _cached;
        private static bool _tried;

        public static TMP_FontAsset Resolve()
        {
            if (_tried) return _cached;
            _tried = true;

            try
            {
                _cached = TMP_Settings.defaultFontAsset;
                if (_cached == null)
                    SlimeLog.Warn("TMP_Settings.defaultFontAsset is null; the label will use TMP's own default");
                else
                    SlimeLog.Info("label font: " + _cached.name);
            }
            catch (Exception ex)
            {
                SlimeLog.Error("could not resolve the game's TMP font", ex);
                _cached = null;
            }

            return _cached;
        }

        public static void Reset()
        {
            _cached = null;
            _tried = false;
        }
    }

    internal static class StarSprite
    {
        private static Sprite _sprite;
        private static bool _tried;

        public static Sprite Get()
        {
            if (_tried) return _sprite;
            _tried = true;

            var tex = Whiten(FromGame());
            if (tex == null)
            {
                tex = Bake(48);
                SlimeLog.Warn("the game's XPerfect star texture could not be read; using a generated star");
            }

            _sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
            _sprite.name = "SlimeKeyViewer.Star";
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            return _sprite;
        }

        private static Texture2D FromGame()
        {
            try
            {
                var type = Type.GetType("RDConstants, Assembly-CSharp", false);
                if (type == null) return null;

                var data = ReadStatic(type, "data");
                if (data == null) return null;

                var prefab = ReadMember(data, "hitTextPrefab") as GameObject;
                if (prefab == null) return null;

                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    var mat = renderer.sharedMaterial;
                    if (mat == null) continue;
                    var tex = mat.mainTexture as Texture2D;
                    if (tex != null && tex.name == "star")
                    {
                        SlimeLog.Info("star particle texture taken from the game (" +
                                      tex.width + "x" + tex.height + ")");
                        return tex;
                    }
                }
            }
            catch (Exception ex)
            {
                SlimeLog.Warn("reading the game's star texture threw: " + ex.Message);
            }
            return null;
        }

        private static Texture2D Whiten(Texture2D source)
        {
            if (source == null) return null;

            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0,
                                                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;

                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    name = "SlimeKeyViewer.StarAlpha",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);

                var pixels = copy.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i].r = 255;
                    pixels[i].g = 255;
                    pixels[i].b = 255;
                }
                copy.SetPixels32(pixels);
                copy.Apply(false, false);
                return copy;
            }
            catch (Exception ex)
            {
                SlimeLog.Warn("could not copy the game's star texture: " + ex.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static object ReadStatic(Type type, string name)
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var prop = type.GetProperty(name, F);
            if (prop != null) return prop.GetValue(null, null);
            var field = type.GetField(name, F);
            return field != null ? field.GetValue(null) : null;
        }

        private static object ReadMember(object target, string name)
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var t = target.GetType();
            var prop = t.GetProperty(name, F);
            if (prop != null) return prop.GetValue(target, null);
            var field = t.GetField(name, F);
            return field != null ? field.GetValue(target) : null;
        }

        private static Texture2D Bake(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var half = size * 0.5f;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - half) / half;
                    var dy = Mathf.Abs(y + 0.5f - half) / half;
                    var v = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
                    var a = Mathf.Clamp01((1.05f - v) * 3f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        public static void Release()
        {
            if (_sprite != null)
            {
                var tex = _sprite.texture;
                UnityEngine.Object.Destroy(_sprite);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            _sprite = null;
            _tried = false;
        }
    }

    internal static class RoundedBox
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Fill(int radius)
        {
            radius = Mathf.Clamp(radius, 0, 100);
            var key = radius * 1000;
            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            var size = Mathf.Max(8, radius * 2 + 4);
            var border = Mathf.Max(1, radius + 1);
            var sprite = Bake(size, radius, -1f, border, "Fill" + radius);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Ring(int radius, int thickness)
        {
            radius = Mathf.Clamp(radius, 0, 100);
            thickness = Mathf.Clamp(thickness, 1, 40);
            var key = radius * 1000 + thickness;
            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            var size = Mathf.Max(8, (radius + thickness) * 2 + 4);
            var border = Mathf.Max(1, radius + thickness + 1);
            var sprite = Bake(size, radius, thickness, border, "Ring" + radius + "_" + thickness);
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Bake(int size, int radius, float thickness, int border, string name)
        {
            var ss = Mathf.Clamp(Mathf.CeilToInt(96f / Mathf.Max(8, size)), 1, 4);

            size *= ss;
            radius *= ss;
            if (thickness >= 0f) thickness *= ss;
            border *= ss;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var half = size * 0.5f;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Distance(x + 0.5f - half, y + 0.5f - half, half, half, radius);

                    var a = Mathf.Clamp01(0.5f - d);

                    if (thickness >= 0f)
                    {
                        a = Mathf.Min(a, Mathf.Clamp01(0.5f + d + thickness));
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f * ss,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = "SlimeKeyViewer." + name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Texture2D Solid(int radius, Color color, out int border)
        {
            radius = Mathf.Clamp(radius, 0, 100);
            var size = Mathf.Max(8, radius * 2 + 4);
            border = Mathf.Max(1, radius + 1);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var half = size * 0.5f;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Distance(x + 0.5f - half, y + 0.5f - half, half, half, radius);
                    var a = Mathf.Clamp01(0.5f - d) * color.a;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        public static Texture2D Circle(int diameter, Color color)
        {
            diameter = Mathf.Clamp(diameter, 2, 128);
            var r = diameter * 0.5f;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[diameter * diameter];
            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x + 0.5f - r;
                    var dy = y + 0.5f - r;
                    var a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f) * color.a;
                    pixels[y * diameter + x] = new Color(color.r, color.g, color.b, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static float Distance(float px, float py, float hx, float hy, float radius)
        {
            var qx = Mathf.Abs(px) - (hx - radius);
            var qy = Mathf.Abs(py) - (hy - radius);
            var outsideX = Mathf.Max(qx, 0f);
            var outsideY = Mathf.Max(qy, 0f);
            return Mathf.Min(Mathf.Max(qx, qy), 0f)
                 + Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                 - radius;
        }

        public static void Release()
        {
            foreach (var sprite in Cache.Values)
            {
                if (sprite == null) continue;
                var tex = sprite.texture;
                UnityEngine.Object.Destroy(sprite);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            Cache.Clear();
        }
    }
}
