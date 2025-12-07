using Newtonsoft.Json;
using SDG.Framework.Modules;
using SDG.Unturned;
using System.IO;
using UnityEngine;
using UnturnedImages.Module.Helpers;
using UnturnedImages.Module.Images;
using UnturnedImages.Module.Models;
using UnturnedImages.Module.Patches;
using UnturnedImages.Module.UI;

namespace UnturnedImages.Module
{
    public class UnturnedImagesModule : MonoBehaviour, IModuleNexus
    {
        private readonly HarmonyPatches _harmonyPatches;
        private readonly UIManager _uiManager;

        public static UnturnedImagesModule? Instance { get; private set; }

        public GameObject? GameObject;

        public UnturnedImagesModule()
        {
            _harmonyPatches = new HarmonyPatches();
            _uiManager = new UIManager();
        }

        public static UnturnedImagesConfig? Config;

        public void initialize()
        {
            UnturnedLog.info("Loading UnturnedImages Module");

            Instance = this;

            // Load config first
            var configPath = Path.Combine(ReadWrite.PATH, "config.json");
            if (File.Exists(configPath))
            {
                string content = File.ReadAllText(configPath);
                Config = JsonConvert.DeserializeObject<UnturnedImagesConfig>(content);
            }
            else
            {
                Config = new UnturnedImagesConfig();
                File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            }

            // Check for crash recovery - if pending_asset.txt exists, we crashed on that asset
            CrashRecoveryHelper.CheckForCrashRecovery();

            GameObject = new GameObject();
            DontDestroyOnLoad(GameObject);

            _harmonyPatches.Patch();
            CustomImageTool.Load();
            CustomVehicleTool.Load();
            AutoStartManager.Load();
            _uiManager.Load();
        }

        public void shutdown()
        {
            UnturnedLog.info("Unloading UnturnedImages Module");

            Destroy(GameObject);

            _uiManager.Unload();
            AutoStartManager.Unload();
            CustomVehicleTool.Unload();
            CustomImageTool.Unload();
            _harmonyPatches.Unpatch();

            Instance = null;
        }
    }
}
