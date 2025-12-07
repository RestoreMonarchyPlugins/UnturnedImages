using HarmonyLib;
using SDG.Unturned;
using System;
using UnityEngine;
using UnturnedImages.Module.Helpers;

namespace UnturnedImages.Module.Patches
{
    /// <summary>
    /// Harmony patches to track item icon generation for crash recovery.
    /// </summary>
    [HarmonyPatch]
    internal static class ItemToolPatches
    {
        // Track the current item being processed
        private static Guid _currentItemGuid;
        private static string? _currentItemName;

        /// <summary>
        /// Prefix patch for ItemTool.captureIcon to mark when we start capturing an item icon.
        /// </summary>
        [HarmonyPatch(typeof(ItemTool), "captureIcon")]
        [HarmonyPrefix]
        public static void CaptureIconPrefix(ushort id)
        {
            try
            {
                var itemAsset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
                if (itemAsset != null)
                {
                    _currentItemGuid = itemAsset.GUID;
                    _currentItemName = itemAsset.itemName;

                    // Mark that we're processing this item
                    CrashRecoveryHelper.MarkProcessingStart(itemAsset.GUID, itemAsset.itemName, "item");
                }
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"Error in CaptureIconPrefix: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch for ItemTool.captureIcon to mark when we finish capturing an item icon.
        /// </summary>
        [HarmonyPatch(typeof(ItemTool), "captureIcon")]
        [HarmonyPostfix]
        public static void CaptureIconPostfix()
        {
            try
            {
                // Mark processing complete
                CrashRecoveryHelper.MarkProcessingComplete();
                _currentItemGuid = Guid.Empty;
                _currentItemName = null;
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"Error in CaptureIconPostfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Finalizer patch to handle exceptions in captureIcon and add to skip list.
        /// </summary>
        [HarmonyPatch(typeof(ItemTool), "captureIcon")]
        [HarmonyFinalizer]
        public static Exception? CaptureIconFinalizer(Exception? __exception)
        {
            if (__exception != null && _currentItemGuid != Guid.Empty)
            {
                UnturnedLog.error($"Exception while capturing icon for item {_currentItemGuid} ({_currentItemName}): {__exception.Message}");
                CrashRecoveryHelper.AddToSkipList(_currentItemGuid, _currentItemName ?? "Unknown");
                CrashRecoveryHelper.MarkProcessingComplete();

                // Return null to suppress the exception and continue processing
                return null;
            }
            return __exception;
        }
    }
}
