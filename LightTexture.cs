#define LFE_DEBUG

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

        private Light _light;
        private static List<string> CookieWrapModes = new List<string> { "Clamp", "Mirror", "MirrorOnce", "Repeat" };

        public override void Init()
        {
            _light = containingAtom.GetComponentInChildren<Light>();
            if (_light == null) { throw new Exception("This must be placed on an Atom with a light"); }

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

            CookieWrapMode = new JSONStorableStringChooser("Texture Wrap Mode", CookieWrapModes, CookieWrapModes.First(), "Texture Wrap Mode", (mode) => {
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

            var loadButton = CreateButton("Load Texture");
            loadButton.button.onClick.AddListener(() => {
                ShowTexturePicker();
            });


            CreateTextField(CookieFilePath).height = 100;
            CreatePopup(CookieWrapMode);

            var clearButton = CreateButton("Clear Texture");
            clearButton.button.onClick.AddListener(() => {
                CookieFilePath.val = "";
            });

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

            path = FileManagerSecure.NormalizePath(path);
            // strip off SELF:/ so that native file actions will work
            if (path.Contains("SELF:/"))
            {
                path = path.Replace("SELF:/", String.Empty);
            }

            // make sure we have a reference to either SELF:/ or SomeVar:/
            // so that the packaging process knows this is special
            var jsonStorablePath = path;
            if (!jsonStorablePath.Contains(":/"))
            {
                jsonStorablePath = $"SELF:/{jsonStorablePath}";
            }

            byte[] file = FileManagerSecure.ReadAllBytes(path);
            Texture cookie = null;

            switch (_light.type)
            {
                case LightType.Area:
                case LightType.Directional:
                case LightType.Spot:
                    cookie = new Texture2D(2, 2);
                    ((Texture2D)cookie).LoadImage(file); // width/heidht is automatic with this
                    break;
                case LightType.Point:
                    // ????
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    // https://gist.github.com/RemyUnity/856f85bfe3ec7a8d845406415b426f87
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    break;
            }

            if (cookie != null)
            {
                _light.cookie = cookie;
                _light.cookie.wrapMode = ParseTextureWrapMode(CookieWrapMode.val);

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
}
