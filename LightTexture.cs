// #define LFE_DEBUG

using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFE
{
    public class LightTexture : MVRScript
    {

        public JSONStorableString CookieFilePath;
        public JSONStorableStringChooser CookieWrapMode;
        public JSONStorableBool GrayscaleToAlpha;
        public JSONStorableBool AddBlackBorder;
        public JSONStorableBool AddVignette;
        public JSONStorableFloat Scale;
        public JSONStorableFloat Brightness;
        public JSONStorableBool Invert;

        private Light _light;
        private static List<string> _cookieWrapModes = new List<string> { "Clamp", "Mirror", "MirrorOnce", "Repeat" };
        private UIDynamicTextField _cookieFilePathUI;

        public override void Init()
        {
            _light = containingAtom.GetComponentInChildren<Light>();
            if (_light == null) { throw new Exception("This must be placed on an Atom with a light"); }


            // LOAD TEXTURE
            var loadButton = CreateButton("Load Texture");
            loadButton.button.onClick.AddListener(() => {
                ShowTexturePicker();
            });


            // GRAYSCALE TO ALPHA
            GrayscaleToAlpha = new JSONStorableBool("Grayscale To Alpha", false, (bool opt) => {
                try
                {
                    SetTexture(CookieFilePath.val);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterBool(GrayscaleToAlpha);
            CreateToggle(GrayscaleToAlpha);


            // ADD BORDER
            AddBlackBorder = new JSONStorableBool("Add Black Border", false, (bool opt) => {
                try
                {
                    SetTexture(CookieFilePath.val);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterBool(AddBlackBorder);
            CreateToggle(AddBlackBorder);


            // ADD VIGNETTE
            AddVignette = new JSONStorableBool("Add Vignette", false, (bool opt) => {
                try
                {
                    SetTexture(CookieFilePath.val);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterBool(AddVignette);
            CreateToggle(AddVignette);


            // INVERT
            Invert = new JSONStorableBool("Invert", false, (bool opt) => {
                try
                {
                    SetTexture(CookieFilePath.val);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterBool(Invert);
            CreateToggle(Invert);


            // BRIGHTNESS
            Brightness = new JSONStorableFloat("Brightness", 0.0f, (float scale) => {
                try
                {
                    SetTexture(CookieFilePath.val);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            }, -1.0f, 1.0f);
            RegisterFloat(Brightness);
            CreateSlider(Brightness);


            // SCALE
            Scale = new JSONStorableFloat("Directional Scale", 1.0f, (float scale) => {
                try
                {
                    _light.cookieSize = Scale.val;
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            }, 0.01f, 5.0f);
            RegisterFloat(Scale);
            CreateSlider(Scale);


            // TEXTURE PATH
            CookieFilePath = new JSONStorableString("Light Texture", String.Empty, (path) => {
                try
                {
                    SetTexture(path);
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterString(CookieFilePath);
            _cookieFilePathUI = CreateTextField(CookieFilePath);
            _cookieFilePathUI.height = 100;


            // WRAP MODE
            CookieWrapMode = new JSONStorableStringChooser("Texture Wrap Mode", _cookieWrapModes, _cookieWrapModes.First(), "Texture Wrap Mode", (mode) => {
                try
                {
                    if (_light.cookie != null)
                    {
                        _light.cookie.wrapMode = ParseTextureWrapMode(mode);
                    }
                }
                catch (Exception e)
                {
                    SuperController.LogError(e.ToString());
                }
            });
            RegisterStringChooser(CookieWrapMode);
            CreatePopup(CookieWrapMode);


            // CLEAR TEXTURE
            var clearButton = CreateButton("Clear Texture");
            clearButton.button.onClick.AddListener(() => {
                CookieFilePath.val = "";
            });


            // INSTRUCTIONS
            var instruction = new JSONStorableString("instructions", String.Empty);
            instruction.val += "Here is where you can load light textures, formally called 'cookies'.\n\n";
            instruction.val += "There are a few included here as an example but you can make your own.\n\n";
            instruction.val += "The only thing Unity will care about is the alpha channel. Color channel will be thrown away.\n\n";
            instruction.val += "1) make a square image in your paint program.\n";
            instruction.val += "2) save it as a square PNG, JPG, or GIF (powers of two are ideal for performance).\n";
            instruction.val += "3) if it is in color, it will be turned into grayscale/alpha if you click the checkbox to the left.\n\n";
            instruction.val += "Tip: add your own square image files in 'Custom/Atom/InvisibleLight/Textures' (even in your own VAR) for easy use";
            CreateTextField(instruction, rightSide: true).height = 1200;

        }

        // use on disable instead of ondestroy -- more predictable when merge loading and stuff
        public void OnDisable() {
            try
            {
                ClearCookie(_light);
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public void OnEnable() {
            try {
                SetTexture(CookieFilePath?.val);
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        // --------------------------------------------------------------------------------

        void SetTexture(string path)
        {

            ClearCookie(_light);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

#if LFE_DEBUG
            SuperController.LogMessage($"path = {path}");
#endif
            path = FileManagerSecure.NormalizePath(path);
#if LFE_DEBUG
            SuperController.LogMessage($"path normalized = {path}");
#endif
            // strip off SELF:/ so that native file actions will work
            if (path.Contains("SELF:/"))
            {
                path = path.Replace("SELF:/", String.Empty);
            }

#if LFE_DEBUG
            SuperController.LogMessage($"path unselfed = {path}");
#endif

            // make sure we have a reference to either SELF:/ or SomeVar:/
            // so that the packaging process knows this is special
            var jsonStorablePath = path;
            if (!jsonStorablePath.Contains(":/"))
            {
                jsonStorablePath = $"SELF:/{jsonStorablePath}";
            }

            byte[] file = FileManagerSecure.ReadAllBytes(path);
#if LFE_DEBUG
            SuperController.LogMessage($"bytes = {file.Length}");
#endif
            Texture cookie = null;

            switch (_light.type)
            {
                case LightType.Area:
                case LightType.Directional:
                case LightType.Spot:
                    cookie = new Texture2D(2, 2, TextureFormat.ARGB32, true, false);
                    ((Texture2D)cookie).LoadImage(file); // width/heidht is automatic with this

                    cookie = ((Texture2D)cookie).EnsureAlphaChannel();

                    if (GrayscaleToAlpha.val)
                    {
                        ((Texture2D)cookie).ApplyGrayscaleAsAlpha();
                    }
                    if (Invert.val)
                    {
                        ((Texture2D)cookie).ApplyInvert();
                    }
                    if (Brightness.val != 0)
                    {
                        ((Texture2D)cookie).ApplyBrightness(Brightness.val);
                    }
                    if (AddBlackBorder.val)
                    {
                        ((Texture2D)cookie).ApplyOverlay($"{GetPluginPath()}Overlays/black-border.png");
                    }
                    if (AddVignette.val)
                    {
                        ((Texture2D)cookie).ApplyOverlay($"{GetPluginPath()}Overlays/vignette-large-soft.png");
                    }
                    break;
                case LightType.Point:
                    cookie = new Texture2D(2, 2, TextureFormat.ARGB32, true, false);
                    ((Texture2D)cookie).LoadImage(file); // width/heidht is automatic with this

                    cookie = ((Texture2D)cookie).EnsureAlphaChannel();

                    if (GrayscaleToAlpha.val)
                    {
                        ((Texture2D)cookie).ApplyGrayscaleAsAlpha();
                    }
                    if (Invert.val)
                    {
                        ((Texture2D)cookie).ApplyInvert();
                    }
                    if (Brightness.val != 0)
                    {
                        ((Texture2D)cookie).ApplyBrightness(Brightness.val);
                    }

                    cookie = ((Texture2D)cookie).ToCubemap();
#if LFE_DEBUG
                    SuperController.LogMessage($"cubemap: {cookie}");
#endif

                    // ????
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    // https://gist.github.com/RemyUnity/856f85bfe3ec7a8d845406415b426f87
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    break;
            }

#if LFE_DEBUG
            if(cookie as Texture2D) {
                var f = ((Texture2D)cookie)?.format;
                SuperController.LogMessage($"cookie = {cookie} format = {f} width = {cookie?.width} height = {cookie?.height}");
            }
            if(cookie as Cubemap) {
                var f = ((Cubemap)cookie)?.format;
                SuperController.LogMessage($"cookie = {cookie} format = {f} width = {cookie?.width} height = {cookie?.height}");
            }
#endif

            if (cookie != null && _light != null)
            {
#if LFE_DEBUG
            SuperController.LogMessage($"light.cookie before = {_light.cookie}");
#endif
                _light.cookie = cookie;
#if LFE_DEBUG
            SuperController.LogMessage($"light.cookie after = {_light.cookie}");
#endif
                if (_light.cookie == null)
                {
                    SuperController.LogError($"{path} is not a valid cookie");
                    SuperController.LogError("Make sure it is a square image for spotlight type lights");
                    CookieFilePath.valNoCallback = String.Empty;
                    return;
                }

                _light.cookie.wrapMode = ParseTextureWrapMode(CookieWrapMode.val);
                _light.cookie.filterMode = FilterMode.Trilinear;
                _light.cookieSize = Scale.val;

                CookieFilePath.valNoCallback = jsonStorablePath;
            }
            else
            {
                SuperController.LogError($"not able to load {path}");
                CookieFilePath.valNoCallback = String.Empty;
            }
        }

        private TextureWrapMode ParseTextureWrapMode(string value)
        {
            switch (value)
            {
                case "Clamp":
                    return TextureWrapMode.Clamp;
                case "Mirror":
                    return TextureWrapMode.Mirror;
                case "MirrorOnce":
                    return TextureWrapMode.MirrorOnce;
                case "Repeat":
                    return TextureWrapMode.Repeat;
                default:
                    return TextureWrapMode.Clamp;
            }
        }

        private void ShowTexturePicker()
        {
            var defaultPaths = new List<string> {
                $"{GetPluginPath()}Textures",
                $"{GetPluginPath()}Custom/Atom/InvisibleLight/Textures",
                $"Custom/Atom/InvisibleLight/Textures"
            };

            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
                "Custom/Atom/InvisibleLight/Textures",
                allowNavigationAboveRegularDirectories: true,
                useFullPaths: true,
                generateAllFlattenedShortcut: true,
                includeRegularDirsInFlattenedShortcut: true
            );

            SuperController.singleton.GetMediaPathDialog(
                (p) => {
                    try
                    {
                        SetTexture(p);
                    }
                    catch (Exception e)
                    {
                        SuperController.LogError(e.ToString());
                    }
                },
                filter: "png|jpg|gif",
                suggestedFolder: defaultPaths.FirstOrDefault(p => FileManagerSecure.DirectoryExists(p)),
                shortCuts: shortcuts,
                showInstallFolderInDirectoryList: true
            );
        }

        private void ClearCookie(Light light)
        {
            if(light?.cookie != null) {
                Destroy(light.cookie);
                light.cookie = null;
            }
        }

        public string GetPluginPath()
        {
            string id = name.Substring(0, name.IndexOf('_'));
            string filename = manager.GetJSON()["plugins"][id].Value;
            string path = filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            return FileManagerSecure.NormalizePath(path);
        }
    }

    public static class Texture2DExtensions
    {

        public static Texture2D EnsureAlphaChannel(this Texture2D source) {
            if(source.format == TextureFormat.ARGB32) {
                return source;
            }
            else {
                var pngBytes = source.EncodeToPNG();
                var png = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true, false);
                png.LoadImage(pngBytes);
                UnityEngine.Object.Destroy(source);
#if LFE_DEBUG
                SuperController.LogMessage($"converted {source.format} to {png.format}");
#endif
                return png;
            }
        }

        public static Texture2D WithBilinearScale(this Texture2D texture, int newWidth, int newHeight)
        {

            var newTexture = new Texture2D(newWidth, newHeight, texture.format, true, false);

            Color[] texColors = texture.GetPixels();
            Color[] newColors = new Color[newWidth * newHeight];

            var ratioX = 1.0f / ((float)newWidth / (texture.width - 1));
            var ratioY = 1.0f / ((float)newHeight / (texture.height - 1));
            for (int y = 0; y < newHeight; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * texture.width;
                var y2 = (yFloor + 1) * texture.width;
                var yw = y * newWidth;
                for (int x = 0; x < newWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                                                       ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                                                       y * ratioY - yFloor);
                }
            }

            newTexture.SetPixels(newColors);
            newTexture.Apply();
            return newTexture;
        }

        public static void ApplyOverlay(this Texture2D rgba, string path)
        {
            path = FileManagerSecure.NormalizePath(path);
            byte[] file = FileManagerSecure.ReadAllBytes(path);


            var overlay = new Texture2D(2, 2, TextureFormat.ARGB32, true, false);
            overlay.LoadImage(file);

            if (overlay.width != rgba.width || overlay.height != overlay.height)
            {
                var resized = overlay.WithBilinearScale(rgba.width, rgba.height);
                UnityEngine.Object.Destroy(overlay);
                overlay = resized;
            }

            var rgbaPixels = rgba.GetPixels();
            var overlayPixels = overlay.GetPixels();

            if(rgbaPixels.Length == overlayPixels.Length) {
                for(var i = 0; i < rgbaPixels.Length; i++) {
                    rgbaPixels[i].a = rgbaPixels[i].a * overlayPixels[i].a;
                }
                rgba.SetPixels(rgbaPixels);
                rgba.Apply();
            }
        }

        public static void ApplyGrayscaleAsAlpha(this Texture2D rgba)
        {
            var pixels = rgba.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i].a = pixels[i].grayscale;
            }
            rgba.SetPixels(pixels);
            rgba.Apply();
        }

        public static void ApplyBrightness(this Texture2D rgba, float brightness)
        {
            if(brightness == 0) {
                return;
            }

            var min = brightness > 0 ? 0.0f + brightness : 0f;
            var max = brightness < 0 ? 1.0f + brightness : 1f;

#if LFE_DEBUG
            SuperController.LogMessage($"min = {min} max = {max}");
#endif
            var pixels = rgba.GetPixels();
            for(int i = 0; i < pixels.Length; i++) {
                pixels[i].a = Mathf.Lerp(min, max, pixels[i].a);
            }
            rgba.SetPixels(pixels);
            rgba.Apply();
        }

        public static void ApplyInvert(this Texture2D rgba)
        {
            var pixels = rgba.GetPixels();
            for(var i = 0; i < pixels.Length; i++) {
                pixels[i].a = 1 - pixels[i].a;
            }
            rgba.SetPixels(pixels);
            rgba.Apply();
        }

        public static Cubemap ToCubemap(this Texture2D rgba) {
            if(rgba == null) {
                return null;
            }
#if LFE_DEBUG
            SuperController.LogMessage($"ToCubeMap({rgba.width} x {rgba.height})");
#endif
            var commonDivisor = GreatestCommonDivisor(rgba.width, rgba.height);
            var ratioW = rgba.width / commonDivisor;
            var ratioH = rgba.height / commonDivisor;

            var ratio = $"{ratioW}:{ratioH}";
#if LFE_DEBUG
            SuperController.LogMessage($"ratio: {ratio}");
#endif
            Cubemap cubemap = null;
            int squareSize = 0;

            // see mappings here: https://docs.unity3d.com/Manual/class-Cubemap.html
            switch(ratio) {
                case "1:1": // if it is square just tile it
                    squareSize = rgba.height;
#if LFE_DEBUG
                    SuperController.LogMessage($"squaresize: {squareSize}");
#endif
                    cubemap = new Cubemap(squareSize, TextureFormat.ARGB32, true);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeX);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveX);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeY);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveY);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeZ);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveZ);
                    cubemap.Apply();
                    break;
                case "6:1":
                    squareSize = rgba.height;
#if LFE_DEBUG
                    SuperController.LogMessage($"squaresize: {squareSize}");
#endif
                    cubemap = new Cubemap(squareSize, TextureFormat.ARGB32, true);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeX);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveX);
                    cubemap.SetPixels(rgba.GetPixels(3 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeY);
                    cubemap.SetPixels(rgba.GetPixels(2 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveY);
                    cubemap.SetPixels(rgba.GetPixels(5 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.NegativeZ);
                    cubemap.SetPixels(rgba.GetPixels(4 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveZ);
                    cubemap.Apply();
                    break;
                case "1:6":
                    squareSize = rgba.width;
#if LFE_DEBUG
                    SuperController.LogMessage($"squaresize: {squareSize}");
#endif
                    cubemap = new Cubemap(squareSize, TextureFormat.ARGB32, true);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.NegativeX);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveX);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 3 * squareSize, squareSize, squareSize), CubemapFace.NegativeY);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 2 * squareSize, squareSize, squareSize), CubemapFace.PositiveY);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 5 * squareSize, squareSize, squareSize), CubemapFace.NegativeZ);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 4 * squareSize, squareSize, squareSize), CubemapFace.PositiveZ);
                    cubemap.Apply();
                    break;
                case "3:4":
                    squareSize = rgba.height / 4;
#if LFE_DEBUG
                    SuperController.LogMessage($"squaresize: {squareSize}");
#endif
                    cubemap = new Cubemap(squareSize, TextureFormat.ARGB32, true);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 3 * squareSize, squareSize, squareSize), CubemapFace.NegativeX);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.PositiveX);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 2 * squareSize, squareSize, squareSize), CubemapFace.NegativeY);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveY);
                    cubemap.SetPixels(rgba.GetPixels(2 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.NegativeZ);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.PositiveZ);
                    cubemap.Apply();
                    break;
                case "4:3":
                    squareSize = rgba.width / 4;
#if LFE_DEBUG
                    SuperController.LogMessage($"squaresize: {squareSize}");
#endif
                    cubemap = new Cubemap(squareSize, TextureFormat.ARGB32, true);
                    cubemap.SetPixels(rgba.GetPixels(0 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.NegativeX);
                    cubemap.SetPixels(rgba.GetPixels(2 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.PositiveX);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 2 * squareSize, squareSize, squareSize), CubemapFace.NegativeY);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 0 * squareSize, squareSize, squareSize), CubemapFace.PositiveY);
                    cubemap.SetPixels(rgba.GetPixels(3 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.NegativeZ);
                    cubemap.SetPixels(rgba.GetPixels(1 * squareSize, 1 * squareSize, squareSize, squareSize), CubemapFace.PositiveZ);
                    cubemap.Apply();
                    break;
                default:
                    SuperController.LogError($"Aspect ratio {ratio} for cubemap texture is not supported.  Must be 1:1, 6:1, 1:6, 4:3,or 3:4");
                    cubemap = null;
                    break;
            }

            return cubemap;
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                            c1.g + (c2.g - c1.g) * value,
                            c1.b + (c2.b - c1.b) * value,
                            c1.a + (c2.a - c1.a) * value);
        }

        private static int GreatestCommonDivisor(int w, int h) {
            while(h != 0) {
                int remainder = w % h;
                w = h;
                h = remainder;
            }
            return w;
        }

    }
}
