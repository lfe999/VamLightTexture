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

        public override void Init() {
            _light = containingAtom.GetComponentInChildren<Light>();
            if(_light == null) { throw new Exception("This must be placed on an Atom with a light"); }

            CookieFilePath = new JSONStorableString("Light Texture", String.Empty, (path) => SetTexture(path));
            CookieWrapMode = new JSONStorableStringChooser("Texture Wrap Mode", CookieWrapModes, CookieWrapModes.First(), "Texture Wrap Mode", (mode) => {
                if(_light.cookie != null) {
                    _light.cookie.wrapMode = ParseTextureWrapMode(mode);
                }
            });

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
            instruction.val += "Tip: make sure the edges are black or else your spotlight will look square";
            CreateTextField(instruction, rightSide: true).height = 1200;

            // trigger the handler for the wrap mode and path in case it was saved in a scene
            CookieWrapMode.val = CookieWrapMode.val;
            CookieFilePath.val = CookieFilePath.val;
        }

        void OnDestroy() {
            ClearCookie(_light);
        }

        // --------------------------------------------------------------------------------

        void SetTexture(string path) {
            ClearCookie(_light);

            if(string.IsNullOrEmpty(path)) {
                return;
            }

            byte[] file = FileManagerSecure.ReadAllBytes(path);
            Texture cookie = null;

            switch(_light.type) {
                case LightType.Area:
                case LightType.Directional:
                case LightType.Spot:
                    cookie = new Texture2D(2, 2);
                    ((Texture2D)cookie).LoadImage(file); // width/height is automatic with this
                    break;
                case LightType.Point:
                    // ????
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    // https://gist.github.com/RemyUnity/856f85bfe3ec7a8d845406415b426f87
                    // https://stackoverflow.com/questions/42746635/from-texture2d-to-cubemap
                    break;
            }

            if(cookie != null) {
                _light.cookie = cookie;
                _light.cookie.wrapMode = ParseTextureWrapMode(CookieWrapMode.val);

                CookieFilePath.val = path;
            }
            else {
                SuperController.LogError($"not able to load {path} (this won't work on point light yet)");
                CookieFilePath.val = "";
            }
        }

        private TextureWrapMode ParseTextureWrapMode(string value) {
            switch(value) {
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

        private void ShowTexturePicker() {
            SuperController.singleton.fileBrowserUI.defaultPath = $"{GetPluginPath()}Textures";
            SuperController.singleton.fileBrowserUI.SetTitle("Select A Light Cookie Texture");
            SuperController.singleton.fileBrowserUI.showFiles = true;
            SuperController.singleton.fileBrowserUI.showDirs = true;
            SuperController.singleton.fileBrowserUI.SetTextEntry(false);
            SuperController.singleton.fileBrowserUI.hideExtension = false;
            SuperController.singleton.fileBrowserUI.showInstallFolderInDirectoryList = true;
            SuperController.singleton.fileBrowserUI.fileFormat = "png";
            SuperController.singleton.fileBrowserUI.Show((string path) => {
                SetTexture(path);
            });
        }

        private void ClearCookie(Light light) {
            if(light.cookie != null) {
                Destroy(light.cookie);
                light.cookie = null;
            }
        }

        public string GetPluginPath() {
            string id = name.Substring(0, name.IndexOf('_'));
            string filename = manager.GetJSON()["plugins"][id].Value;
            string path = filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            return path;
        }
    }
}
