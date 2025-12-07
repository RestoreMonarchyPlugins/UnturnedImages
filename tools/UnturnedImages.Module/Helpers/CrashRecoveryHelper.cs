using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnturnedImages.Module.Models;

namespace UnturnedImages.Module.Helpers
{
    public static class CrashRecoveryHelper
    {
        private const string PendingAssetFile = "pending_asset.txt";

        // Track currently processing asset
        private static Guid? _currentlyProcessingGuid;
        private static string? _currentlyProcessingName;
        private static string? _currentlyProcessingType;

        /// <summary>
        /// Check for crash recovery on module load. If pending_asset.txt exists,
        /// the previous session crashed while processing that asset.
        /// </summary>
        public static void CheckForCrashRecovery()
        {
            var pendingFile = Path.Combine(ReadWrite.PATH, PendingAssetFile);
            if (!File.Exists(pendingFile))
                return;

            try
            {
                var lines = File.ReadAllLines(pendingFile);
                if (lines.Length >= 3 && Guid.TryParse(lines[0], out var crashedGuid))
                {
                    var assetName = lines[1];
                    var assetType = lines[2];

                    UnturnedLog.warn($"Detected previous crash while processing {assetType}: {crashedGuid} ({assetName})");
                    UnturnedLog.warn("Automatically adding to skip list...");

                    AddToSkipList(crashedGuid, assetName);
                }
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"Error reading crash recovery file: {ex.Message}");
            }
            finally
            {
                // Always delete the pending file
                try { File.Delete(pendingFile); } catch { }
            }
        }

        /// <summary>
        /// Add a GUID to the skip list and save the config.
        /// </summary>
        public static void AddToSkipList(Guid guid, string assetName)
        {
            if (UnturnedImagesModule.Config == null)
            {
                UnturnedImagesModule.Config = new UnturnedImagesConfig();
            }

            var skipList = UnturnedImagesModule.Config.SkipGuids?.ToList() ?? new List<Guid>();

            if (!skipList.Contains(guid))
            {
                skipList.Add(guid);
                UnturnedImagesModule.Config.SkipGuids = skipList.ToArray();

                // Save the config
                try
                {
                    var configPath = Path.Combine(ReadWrite.PATH, "config.json");
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(UnturnedImagesModule.Config, Formatting.Indented));
                    UnturnedLog.info($"Added {guid} ({assetName}) to skip list and saved config.");
                }
                catch (Exception ex)
                {
                    UnturnedLog.error($"Failed to save config: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Mark that we're starting to process an asset.
        /// </summary>
        public static void MarkProcessingStart(Guid guid, string assetName, string assetType)
        {
            _currentlyProcessingGuid = guid;
            _currentlyProcessingName = assetName;
            _currentlyProcessingType = assetType;

            // Write to a file so we know if the game crashes
            var pendingFile = Path.Combine(ReadWrite.PATH, PendingAssetFile);
            try
            {
                File.WriteAllText(pendingFile, $"{guid}\n{assetName}\n{assetType}");
            }
            catch { }
        }

        /// <summary>
        /// Mark that we've finished processing the current asset.
        /// </summary>
        public static void MarkProcessingComplete()
        {
            _currentlyProcessingGuid = null;
            _currentlyProcessingName = null;
            _currentlyProcessingType = null;

            // Delete the pending file
            var pendingFile = Path.Combine(ReadWrite.PATH, PendingAssetFile);
            try { File.Delete(pendingFile); } catch { }
        }

        /// <summary>
        /// Check if an asset should be skipped.
        /// </summary>
        public static bool ShouldSkip(Guid guid)
        {
            return UnturnedImagesModule.Config?.SkipGuids != null &&
                   UnturnedImagesModule.Config.SkipGuids.Contains(guid);
        }

        /// <summary>
        /// Get info about the currently processing asset.
        /// </summary>
        public static (Guid? guid, string? name, string? type) GetCurrentlyProcessing()
        {
            return (_currentlyProcessingGuid, _currentlyProcessingName, _currentlyProcessingType);
        }
    }
}
