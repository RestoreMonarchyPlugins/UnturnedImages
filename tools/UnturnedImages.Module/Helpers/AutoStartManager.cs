using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnturnedImages.Module.Images;
using UnturnedImages.Module.Models;

namespace UnturnedImages.Module.Helpers
{
    /// <summary>
    /// Manages automatic icon generation on startup.
    /// </summary>
    public class AutoStartManager : MonoBehaviour
    {
        private static AutoStartManager? _instance;
        private bool _hasStarted = false;
        private bool _isGenerating = false;
        private float _checkInterval = 1f;
        private float _lastCheckTime = 0f;

        public static void Load()
        {
            if (UnturnedImagesModule.Instance?.GameObject == null)
                return;

            _instance = UnturnedImagesModule.Instance.GameObject.AddComponent<AutoStartManager>();
        }

        public static void Unload()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }

        private void Start()
        {
            // Start generation after delay from menu
            var config = UnturnedImagesModule.Config?.AutoStart;
            if (config == null || !config.Enabled)
            {
                UnturnedLog.info("[AutoStart] AutoStart is disabled or not configured.");
                return;
            }

            UnturnedLog.info("[AutoStart] AutoStart is enabled. Waiting for assets to load...");
            StartCoroutine(WaitForAssetsAndStart(config));
        }

        private IEnumerator WaitForAssetsAndStart(AutoStartConfig config)
        {
            // Wait for assets to be fully loaded
            while (Assets.isLoading)
            {
                yield return new WaitForSeconds(1f);
            }

            UnturnedLog.info("[AutoStart] Assets loaded. Will start after delay...");
            yield return StartGenerationAfterDelay(config);
        }

        private void OnDestroy()
        {
            // Nothing to unsubscribe
        }

        private IEnumerator StartGenerationAfterDelay(AutoStartConfig config)
        {
            UnturnedLog.info($"[AutoStart] Waiting {config.StartDelaySeconds} seconds before starting icon generation...");
            yield return new WaitForSeconds(config.StartDelaySeconds);

            UnturnedLog.info("[AutoStart] Starting automatic icon generation...");
            _isGenerating = true;

            // Get angles
            Vector3? itemAngles = null;
            Vector3? vehicleAngles = null;

            if (config.ItemAngles != null && config.ItemAngles.Length >= 3)
                itemAngles = new Vector3(config.ItemAngles[0], config.ItemAngles[1], config.ItemAngles[2]);

            if (config.VehicleAngles != null && config.VehicleAngles.Length >= 3)
                vehicleAngles = new Vector3(config.VehicleAngles[0], config.VehicleAngles[1], config.VehicleAngles[2]);

            // Create extras directories
            IconUtils.CreateExtrasDirectory();
            ReadWrite.createFolder("/Extras/Items");
            ReadWrite.createFolder("/Extras/Vehicles");

            try
            {
                switch (config.Mode?.ToLowerInvariant())
                {
                    case "items":
                        UnturnedLog.info("[AutoStart] Generating all item icons...");
                        ImageUtils.CaptureAllItemImages(itemAngles);
                        break;

                    case "vehicles":
                        UnturnedLog.info("[AutoStart] Generating all vehicle icons...");
                        ImageUtils.CaptureAllVehicleImages(vehicleAngles);
                        break;

                    case "mod":
                        if (config.ModId.HasValue)
                        {
                            UnturnedLog.info($"[AutoStart] Generating icons for mod {config.ModId.Value}...");
                            if (config.GenerateItems)
                                ImageUtils.CaptureModItemImages(config.ModId.Value, itemAngles);
                            if (config.GenerateVehicles)
                                ImageUtils.CaptureModVehicleImages(config.ModId.Value, vehicleAngles);
                        }
                        else
                        {
                            UnturnedLog.warn("[AutoStart] Mode is 'mod' but no ModId specified!");
                        }
                        break;

                    case "all":
                    default:
                        UnturnedLog.info("[AutoStart] Generating all icons...");
                        if (config.GenerateItems)
                            ImageUtils.CaptureAllItemImages(itemAngles);
                        if (config.GenerateVehicles)
                            ImageUtils.CaptureAllVehicleImages(vehicleAngles);
                        break;
                }
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"[AutoStart] Error during icon generation: {ex.Message}");
            }

            UnturnedLog.info("[AutoStart] Icon generation queued. Monitoring progress...");
        }

        private void Update()
        {
            if (!_isGenerating)
                return;

            // Check periodically if generation is complete
            if (Time.time - _lastCheckTime < _checkInterval)
                return;

            _lastCheckTime = Time.time;

            // Check if there are any pending icons
            int pendingItems = IconUtils.extraIcons.Count;
            int pendingVehicles = GetPendingVehicleCount();

            if (pendingItems == 0 && pendingVehicles == 0)
            {
                _isGenerating = false;
                OnGenerationComplete();
            }
        }

        private int GetPendingVehicleCount()
        {
            // Use reflection to get the queue count from CustomVehicleTool
            try
            {
                var field = typeof(CustomVehicleTool).GetField("Icons",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    // We need to get the instance - it's a MonoBehaviour so we find it
                    var instance = FindObjectOfType<CustomVehicleTool>();
                    if (instance != null)
                    {
                        var queue = field.GetValue(instance) as System.Collections.ICollection;
                        return queue?.Count ?? 0;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            return 0;
        }

        private void OnGenerationComplete()
        {
            UnturnedLog.info("[AutoStart] Icon generation complete!");

            // Write a completion marker file for the external script
            var completionFile = Path.Combine(ReadWrite.PATH, "generation_complete.txt");
            try
            {
                File.WriteAllText(completionFile, DateTime.Now.ToString("o"));
            }
            catch { }

            var config = UnturnedImagesModule.Config?.AutoStart;
            if (config?.QuitWhenDone == true)
            {
                UnturnedLog.info("[AutoStart] QuitWhenDone is enabled. Quitting game...");
                Application.Quit();
            }
        }
    }
}
