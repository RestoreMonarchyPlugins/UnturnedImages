using System;

namespace UnturnedImages.Module.Models
{
    public class UnturnedImagesConfig
    {
        /// <summary>
        /// List of asset GUIDs to skip when generating icons.
        /// Automatically populated when assets cause crashes.
        /// </summary>
        public Guid[]? SkipGuids { get; set; } = Array.Empty<Guid>();

        /// <summary>
        /// Auto-start configuration for unattended icon generation.
        /// </summary>
        public AutoStartConfig? AutoStart { get; set; }
    }

    public class AutoStartConfig
    {
        /// <summary>
        /// Whether to automatically start generating icons when the game loads.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// What to generate: "all", "items", "vehicles", or "mod"
        /// </summary>
        public string Mode { get; set; } = "all";

        /// <summary>
        /// If Mode is "mod", specify the workshop mod ID to generate icons for.
        /// </summary>
        public ulong? ModId { get; set; }

        /// <summary>
        /// Whether to generate item icons (when Mode is "all" or "mod").
        /// </summary>
        public bool GenerateItems { get; set; } = true;

        /// <summary>
        /// Whether to generate vehicle icons (when Mode is "all" or "mod").
        /// </summary>
        public bool GenerateVehicles { get; set; } = true;

        /// <summary>
        /// Custom rotation angles for item icons (X, Y, Z).
        /// </summary>
        public float[]? ItemAngles { get; set; }

        /// <summary>
        /// Custom rotation angles for vehicle icons (X, Y, Z).
        /// </summary>
        public float[]? VehicleAngles { get; set; }

        /// <summary>
        /// Whether to automatically quit the game when icon generation is complete.
        /// Useful for automation scripts.
        /// </summary>
        public bool QuitWhenDone { get; set; } = false;

        /// <summary>
        /// Delay in seconds before starting icon generation after game loads.
        /// </summary>
        public float StartDelaySeconds { get; set; } = 5f;
    }
}
