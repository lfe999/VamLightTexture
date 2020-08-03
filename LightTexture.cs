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
        public JSONStorableFloat Scale;

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
            instruction.val += "2) paint in greyscale, while will be bright, black will be dark.\n";
            instruction.val += "3) use an alpha from greyscale tool in your paint program.\n";
            instruction.val += "4) save it as a square PNG making sure the width and height are powers of two.\n\n";
            instruction.val += "Tip: make sure the edges are black or else your spotlight will look square\n\n";
            instruction.val += "Tip: add your own PNG files in 'Custom/Atom/InvisibleLight/Textures' (even in your own VAR) for easy use";
            CreateTextField(instruction, rightSide: true).height = 1200;

        }

        void OnDestroy()
        {
            try
            {
                ClearCookie(_light);
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
                    cookie = new Texture2D(2, 2);
                    ((Texture2D)cookie).LoadImage(file); // width/heidht is automatic with this
                    if(GrayscaleToAlpha.val) {
                        Texture2D modified = ((Texture2D)cookie).WithGrayscaleAsAlpha();
                        Destroy(cookie);
                        cookie = modified;
                    }
                    break;
                case LightType.Point:
                    // ????
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    // https://gist.github.com/RemyUnity/856f85bfe3ec7a8d845406415b426f87
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    break;
            }

#if LFE_DEBUG
            SuperController.LogMessage($"cookie = {cookie} width = {cookie.width} height = {cookie.height}");
#endif

            if (cookie != null)
            {
#if LFE_DEBUG
            SuperController.LogMessage($"light.cookie before = {_light.cookie}");
#endif
                _light.cookie = cookie;
#if LFE_DEBUG
            SuperController.LogMessage($"light.cookie after = {_light.cookie}");
#endif
                if(_light.cookie == null) {
                    SuperController.LogError($"{path} is not a valid cookie");
                    SuperController.LogError("Make sure it is a square image");
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
                SuperController.LogError($"not able to load {path} (this won't work on point light yet)");
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
                filter: "png",
                suggestedFolder: defaultPaths.FirstOrDefault(p => FileManagerSecure.DirectoryExists(p)),
                shortCuts: shortcuts,
                showInstallFolderInDirectoryList: true
            );
        }

        private void ClearCookie(Light light)
        {
            if (light.cookie != null)
            {
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

    public static class Texture2DExtensions {
        public static Texture2D WithGrayscaleAsAlpha(this Texture2D rgba) {
            var grayscale = new Texture2D(rgba.width, rgba.height);
            Graphics.CopyTexture(rgba, grayscale);

            for(int y = 0; y < grayscale.height; y++) {
                for(int x = 0; x < grayscale.width; x++) {
                    var pixel = grayscale.GetPixel(x, y);
                    var l = ((0.2126f * pixel.r) + (0.7152f * pixel.g) + (0.0722f * pixel.b));
                    grayscale.SetPixel(x, y, new Color(l, l, l, l));
                }
            }
            grayscale.Apply();

            return grayscale;
        }
    }
}
